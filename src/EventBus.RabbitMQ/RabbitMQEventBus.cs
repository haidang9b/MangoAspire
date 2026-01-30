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
using System.Collections.Concurrent;
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
    RabbitMQTelemetry rabbitMQTelemetry)
    : IEventBus, IDisposable, IHostedService
{
    private const string ExchangeName = "mango_event_bus";
    private readonly IConnection _rabbitMQConnection = serviceProvider.GetRequiredService<IConnection>();
    private readonly EventBusSubscriptionInfo _eventBusSubscriptionInfo = subscriptionOptions.Value;
    private readonly ActivitySource _activitySource = rabbitMQTelemetry.ActivitySource;
    private readonly ResiliencePipeline _pipeline = CreateResiliencePipeline(options.Value.RetryCount);
    private readonly TextMapPropagator _propagator = rabbitMQTelemetry.Propagator;
    private readonly ConcurrentDictionary<string, IChannel> _consumerChannels = new();
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
            foreach (var channel in _consumerChannels.Values)
            {
                channel?.Dispose();
            }
            _consumerChannels.Clear();
        }

        _disposed = true;
    }

    public async Task PublishAsync(IntegrationEvent @event)
    {
        await _pipeline.Execute(async () =>
        {
            var eventType = @event.GetType();
            var eventName = eventType.Name;
            var activityName = $"{eventName} publish";

            using var activity = _activitySource.StartActivity(activityName, ActivityKind.Client);

            ActivityContext contextToInject = default;
            if (activity != null)
            {
                contextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }

            var messageBody = JsonSerializer.Serialize(@event, _eventBusSubscriptionInfo.JsonSerializerOptions);
            var body = Encoding.UTF8.GetBytes(messageBody);

            using var channel = await _rabbitMQConnection.CreateChannelAsync();

            // Declare exchange if not exists
            await channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Direct);

            var properties = new BasicProperties();
            static void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
            {
                props.Headers ??= new Dictionary<string, object>();
                props.Headers[key] = value;
            }
            var senderNames = _eventBusSubscriptionInfo.TopicTypes
            .Where(x => x.Value == eventType)
            .Select(x => x.Key)
            .Concat(_eventBusSubscriptionInfo.QueueTypes
                .Where(x => x.Value == eventType)
                .Select(x => x.Key))
            .Distinct()
            .ToList();

            if (!senderNames.Any())
            {
                return;
            }

            _propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, InjectTraceContextIntoBasicProperties);

            SetActivityContext(activity, eventName, "publish");

            try
            {
                foreach (var senderName in senderNames)
                {
                    await channel.BasicPublishAsync(
                       exchange: ExchangeName,
                       routingKey: senderName,
                       mandatory: true,
                       basicProperties: properties,
                       body: body);
                }
            }
            catch (Exception ex)
            {
                activity.SetExceptionTags(ex);

                throw;
            }
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(async () =>
        {
            try
            {
                //// Subscriptions (Topics)
                //foreach (var subscription in _eventBusSubscriptionInfo.SubscriptionTypes)
                //{
                //    var queueName = subscription.Key.SubscriptionName;
                //    var topicName = subscription.Key.TopicName;
                //    var eventType = subscription.Value;
                //    var handlerKey = subscription.Key.ToString();

                //    await StartConsumerAsync(queueName, topicName, eventType, handlerKey, isTopic: true);
                //}

                // Consumers (Queues)
                foreach (var queue in _eventBusSubscriptionInfo.ConsumerTypes)
                {
                    var queueName = queue.Key;
                    var eventType = queue.Value;
                    var handlerKey = new ConsumerInfo(queue.Key).ToString();

                    await StartConsumerAsync(queueName, routingKey: queueName, eventType, handlerKey, isTopic: false);
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

    private async Task StartConsumerAsync(string queueName, string routingKey, Type eventType, string handlerKey, bool isTopic)
    {
        try 
        {
            var channel = await _rabbitMQConnection.CreateChannelAsync();
            _consumerChannels.TryAdd(queueName, channel);

            channel.CallbackExceptionAsync += (sender, ea) =>
            {
                logger.LogWarning(ea.Exception, "Error with RabbitMQ consumer channel for {QueueName}", queueName);
                return Task.CompletedTask;
            };

            await channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Direct);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            //if (isTopic)
            //{
            //    await channel.QueueBindAsync(
            //        queue: queueName,
            //        exchange: ExchangeName,
            //        routingKey: routingKey);
            //}
            // If not topic (direct queue), we might not bind to exchange or bind with queue name as routing key.
            // ServiceBus "Queue" usually means Point-to-Point. 
            // Here we bind it to the exchange with the queue name as routing key so we can publish to it via the exchange.
            // OR we could just consume from the queue if publishers publish directly to queue (default exchange).
            // But PublishAsync publishes to 'ExchangeName' with 'senderName'. 
            // So we MUST bind.
            await channel.QueueBindAsync(
                   queue: queueName,
                   exchange: ExchangeName,
                   routingKey: routingKey);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Starting RabbitMQ basic consume for {QueueName}", queueName);
            }

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (sender, args) => 
            {
                await OnMessageReceived(sender, args, eventType, handlerKey);
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating consumer for {QueueName}", queueName);
        }
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs, Type eventType, string handlerKey)
    {
        static IEnumerable<string> ExtractTraceContextFromBasicProperties(IReadOnlyBasicProperties props, string key)
        {
            if (props.Headers.TryGetValue(key, out var value))
            {
                var bytes = value as byte[];
                return [Encoding.UTF8.GetString(bytes)];
            }
            return [];
        }

        var parentContext = _propagator.Extract(default, eventArgs.BasicProperties, ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;

        var activityName = $"{eventArgs.RoutingKey} receive";

        using var activity = _activitySource.StartActivity(activityName, ActivityKind.Client, parentContext.ActivityContext);

        SetActivityContext(activity, eventArgs.RoutingKey, "receive");

        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            activity?.SetTag("message", message);

            await ProcessEvent(message, eventType, handlerKey);
            
            // Ack after successful processing
            if (sender is AsyncEventingBasicConsumer consumer && consumer.Channel is IChannel channel)
            {
                 await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error Processing message \"{Message}\"", message);
            activity.SetExceptionTags(ex);
            
            // Nack or similar could be done here
            if (sender is AsyncEventingBasicConsumer consumer && consumer.Channel is IChannel channel)
            {
                 // Requeue = true? or false/DLX?
                 await channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
            }
        }
    }

    private async Task ProcessEvent(string message, Type eventType, string handlerKey)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Processing RabbitMQ event for handler {HandlerKey}", handlerKey);
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        
        var integrationEvent = DeserializeMessage(message, eventType);

        if (integrationEvent != null)
        {
            var handler = scope.ServiceProvider.GetRequiredKeyedService<IIntegrationEventHandler>(handlerKey);
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
        if (activity is not null)
        {
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.destination_kind", "queue");
            activity.SetTag("messaging.operation", operation);
            activity.SetTag("messaging.destination.name", routingKey);
        }
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
        foreach (var channel in _consumerChannels.Values)
        {
             await channel.CloseAsync(cancellationToken);
        }
    }
}
