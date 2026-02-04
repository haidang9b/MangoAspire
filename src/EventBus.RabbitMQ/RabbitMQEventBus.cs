using EventBus.Abstractions;
using EventBus.Events;
using Mango.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace EventBus.RabbitMQ;

public sealed class RabbitMQEventBus(
    ILogger<RabbitMQEventBus> logger,
    IServiceProvider serviceProvider,
    IOptions<EventBusOptions> options,
    IOptions<EventBusSubscriptionInfo> subscriptionOptions,
    IOptions<RabbitMQInfo> rabbitMQoptions,
    RabbitMQTelemetry rabbitMQTelemetry)
    : IEventBus, IDisposable, IHostedService
{
    private IConnection _rabbitMQConnection;
    private readonly EventBusSubscriptionInfo _eventBusSubscriptionInfo = subscriptionOptions.Value;
    private readonly ActivitySource _activitySource = rabbitMQTelemetry.ActivitySource;
    private readonly ResiliencePipeline _pipeline = CreateResiliencePipeline(options.Value.RetryCount);
    private readonly TextMapPropagator _propagator = rabbitMQTelemetry.Propagator;

    private readonly RabbitMQInfo _rabbitMQInfo = rabbitMQoptions.Value;
    private IChannel? _consumerChannel;
    private IChannel? _publishChannel;
    private readonly SemaphoreSlim _publishSemaphore = new(1, 1);
    private readonly string _queueName = options.Value.SubscriptionClientName;
    private readonly string _exchangeName = options.Value.DomainName;

    private bool _disposed;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _consumerChannel?.Dispose();
            _publishChannel?.Dispose();
            _publishSemaphore.Dispose();
        }

        _disposed = true;
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        var routingKey = @event.GetType().Name;

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Creating RabbitMQ channel to publish event: ({EventName})", routingKey);
        }

        var messageBody = JsonSerializer.Serialize(@event, _eventBusSubscriptionInfo.JsonSerializerOptions);
        var body = Encoding.UTF8.GetBytes(messageBody);

        var activityName = $"{routingKey} publish";

        await _pipeline.Execute(async () =>
        {
            using var activity = _activitySource.StartActivity(activityName, ActivityKind.Client);

            // Depending on Sampling (and whether a listener is registered or not), the activity above may not be created.
            // If it is created, then propagate its context. If it is not created, the propagate the Current context, if any.

            ActivityContext contextToInject = default;
            if (activity != null)
            {
                contextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }

            var properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };

            static void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
            {
                props.Headers ??= new Dictionary<string, object?>();
                props.Headers[key] = value;
            }

            _propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, InjectTraceContextIntoBasicProperties);

            SetActivityContext(activity, routingKey, "publish");

            await _publishSemaphore.WaitAsync();
            try
            {
                if (_publishChannel is null || !_publishChannel.IsOpen)
                {
                    _rabbitMQConnection = serviceProvider.GetRequiredService<IConnection>();
                    _publishChannel = await _rabbitMQConnection.CreateChannelAsync();

                    // Declare exchange only when re-creating channel to ensure it exists
                    await _publishChannel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Direct);
                }

                await _publishChannel.BasicPublishAsync(
                    exchange: _exchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            }
            catch (Exception ex)
            {
                activity?.SetExceptionTags(ex);
                // Force channel re-creation on error
                _publishChannel?.Dispose();
                _publishChannel = null;
                throw;
            }
            finally
            {
                _publishSemaphore.Release();
            }
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Messaging is async so we don't need to wait for it to complete.
        Task.Factory.StartNew(async () =>
        {
            try
            {
                logger.LogInformation("Starting RabbitMQ connection on a background thread");

                _rabbitMQConnection = serviceProvider.GetRequiredService<IConnection>();

                // Retry until connection is open using ResiliencePipeline
                var pipeline = new ResiliencePipelineBuilder()
                    .AddRetry(new RetryStrategyOptions
                    {
                        ShouldHandle = new PredicateBuilder().Handle<Exception>().Handle<SocketException>(),
                        MaxRetryAttempts = int.MaxValue,
                        DelayGenerator = context => ValueTask.FromResult((TimeSpan?)TimeSpan.FromSeconds(Math.Min(Math.Pow(2, context.AttemptNumber), 30))),
                        OnRetry = args =>
                        {
                            logger.LogWarning(args.Outcome.Exception, "Could not connect to RabbitMQ, retrying in {TimeOut}s", args.RetryDelay.TotalSeconds);
                            return ValueTask.CompletedTask;
                        }
                    })
                    .Build();

                await pipeline.ExecuteAsync(async ct =>
                {
                    if (!_rabbitMQConnection.IsOpen)
                    {
                        throw new SocketException((int)SocketError.NotConnected);
                    }
                    await Task.CompletedTask;
                }, cancellationToken);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Creating RabbitMQ consumer channel");
                }

                _consumerChannel = await _rabbitMQConnection.CreateChannelAsync();

                _consumerChannel.CallbackExceptionAsync += (sender, ea) =>
                {
                    logger.LogWarning(ea.Exception, "Error with RabbitMQ consumer channel");
                    return Task.CompletedTask;
                };

                await _consumerChannel.ExchangeDeclareAsync(
                    exchange: _exchangeName,
                    type: "direct");

                await _consumerChannel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Starting RabbitMQ basic consume");
                }

                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.ReceivedAsync += OnMessageReceived;

                await _consumerChannel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);

                foreach (var item in _rabbitMQInfo.EventTypes)
                {
                    // Make sure the exchange is created
                    await _consumerChannel.ExchangeDeclareAsync(exchange: item.Key, type: ExchangeType.Direct);

                    await _consumerChannel.QueueBindAsync(
                        queue: _queueName,
                        exchange: item.Key,
                        routingKey: item.Value.Name);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting RabbitMQ connection");
            }
        },
        TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        static IEnumerable<string> ExtractTraceContextFromBasicProperties(IReadOnlyBasicProperties props, string key)
        {
            if (props.Headers!.TryGetValue(key, out var value))
            {
                var bytes = value as byte[];
                return [Encoding.UTF8.GetString(bytes)];
            }
            return [];
        }

        // Extract the PropagationContext of the upstream parent from the message headers.
        var parentContext = _propagator.Extract(default, eventArgs.BasicProperties, ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;

        // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md
        var activityName = $"{eventArgs.RoutingKey} receive";

        using var activity = _activitySource.StartActivity(activityName, ActivityKind.Client, parentContext.ActivityContext);

        SetActivityContext(activity, eventArgs.RoutingKey, "receive");

        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            activity?.SetTag("message", message);

            if (message.Contains("throw-fake-exception", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }

            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error Processing message \"{Message}\"", message);

            activity?.SetExceptionTags(ex);
        }

        // Even on exception we take the message off the queue.
        // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
        // For more information see: https://www.rabbitmq.com/dlx.html
        if (_consumerChannel != null)
        {
            await _consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        }
    }
    private async Task ProcessEvent(string eventName, string message)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var eventType = _rabbitMQInfo.EventTypes.FirstOrDefault(x => x.Value.Name == eventName).Value;
        if (eventType == null)
        {
            logger.LogWarning("Unable to resolve event type for event name {EventName}", eventName);
            return;
        }

        // Deserialize the event
        var integrationEvent = DeserializeMessage(message, eventType);

        // Get all the handlers using the event type as the key
        var handlers = scope.ServiceProvider.GetKeyedServices<IIntegrationEventHandler>(eventType);
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(integrationEvent);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
    Justification = "The 'JsonSerializer.IsReflectionEnabledByDefault' feature switch, which is set to false by default for trimmed .NET apps, ensures the JsonSerializer doesn't use Reflection.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "See above.")]
    private IntegrationEvent? DeserializeMessage(string message, Type eventType)
    {
        return JsonSerializer.Deserialize(message, eventType, _eventBusSubscriptionInfo.JsonSerializerOptions) as IntegrationEvent;
    }

    private static void SetActivityContext(Activity? activity, string routingKey, string operation)
    {
        if (activity is null) return;

        // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md
        activity.SetTag("messaging.system", "rabbitmq");
        activity.SetTag("messaging.destination_kind", "queue");
        activity.SetTag("messaging.operation", operation);
        activity.SetTag("messaging.destination.name", routingKey);
        activity.SetTag("messaging.rabbitmq.routing_key", routingKey);
    }

    private static ResiliencePipeline CreateResiliencePipeline(int retryCount)
    {
        var retryOptions = new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>().Handle<SocketException>(),
            MaxRetryAttempts = retryCount,
            DelayGenerator = (context) => ValueTask.FromResult(GenerateDelay(context.AttemptNumber))
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .Build();

        static TimeSpan? GenerateDelay(int attempt)
        {
            return TimeSpan.FromSeconds(Math.Pow(2, attempt));
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_consumerChannel != null)
        {
            await _consumerChannel.CloseAsync(cancellationToken);
        }

        if (_publishChannel != null)
        {
            await _publishChannel.CloseAsync(cancellationToken);
        }
    }
}
