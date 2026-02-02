using Azure.Messaging.ServiceBus;
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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace EventBus.ServiceBus;

public sealed class ServiceBusEventBus(
    ILogger<ServiceBusEventBus> logger,
    IServiceProvider serviceProvider,
    IOptions<EventBusOptions> options,
    IOptions<EventBusSubscriptionInfo> subscriptionOptions,
    ServiceBusTelemetry serviceBusTelemetry,
    ServiceBusClient serviceBusClient
    )
    : IEventBus, IDisposable, IAsyncDisposable, IHostedService
{
    private readonly EventBusSubscriptionInfo _eventBusSubscriptionInfo = subscriptionOptions.Value;
    private readonly ActivitySource _activitySource = serviceBusTelemetry.ActivitySource;
    private readonly ResiliencePipeline _pipeline = CreateResiliencePipeline(options.Value.RetryCount);
    private readonly TextMapPropagator _propagator = serviceBusTelemetry.Propagator;
    private readonly List<ServiceBusProcessor> _processors = [];
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();
    private bool _disposed;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var processor in _processors)
        {
            await processor.DisposeAsync();
        }
        _processors.Clear();

        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
        _senders.Clear();
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _disposed = true;
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        await _pipeline.Execute(async () =>
        {
            var eventType = @event.GetType();
            var activityName = $"{eventType.Name} publish";

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

            var messageBody = JsonSerializer.Serialize(@event, _eventBusSubscriptionInfo.JsonSerializerOptions);

            foreach (var destination in senderNames)
            {
                try
                {
                    SetActivityContext(activity, destination, "publish", destinationKind: "queue");

                    var sender = _senders.GetOrAdd(destination, _ => serviceBusClient.CreateSender(destination));

                    var sbMessage = new ServiceBusMessage(messageBody)
                    {
                        Subject = eventType.Name,
                        ContentType = "application/json"
                    };

                    _propagator.Inject(
                        new PropagationContext(contextToInject, Baggage.Current),
                        sbMessage,
                        (msg, key, value) => msg.ApplicationProperties[key] = value);

                    await sender.SendMessageAsync(sbMessage);
                }
                catch (Exception ex)
                {
                    activity?.SetExceptionTags(ex);
                    throw;
                }
            }
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in _eventBusSubscriptionInfo.SubscriptionTypes)
        {
            var processor = serviceBusClient.CreateProcessor(
                subscription.Key.TopicName,
                subscription.Key.SubscriptionName,
                new ServiceBusProcessorOptions
                {
                    AutoCompleteMessages = false,
                    MaxConcurrentCalls = 1
                });

            var eventType = subscription.Value;
            var handlerKey = new SubscriptionInfo(subscription.Key.TopicName, subscription.Key.SubscriptionName);

            processor.ProcessMessageAsync += async (args) =>
            {
                await ProcessMessageAsync(args, eventType, handlerKey.ToString());
            };
            processor.ProcessErrorAsync += OnProcessError;

            _processors.Add(processor);

            await processor.StartProcessingAsync(cancellationToken);
        }

        foreach (var queue in _eventBusSubscriptionInfo.ConsumerTypes)
        {
            var processor = serviceBusClient.CreateProcessor(
            queue.Key,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

            var eventType = queue.Value;
            var handlerKey = new ConsumerInfo(queue.Key);

            processor.ProcessMessageAsync += async (args) =>
                await ProcessMessageAsync(args, eventType, handlerKey.ToString());

            processor.ProcessErrorAsync += OnProcessError;

            _processors.Add(processor);

            await processor.StartProcessingAsync(cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args, Type eventType, string handlerKey)
    {
        var message = args.Message;

        var parentContext = _propagator.Extract(
            default,
            message,
            (msg, key) =>
                msg.ApplicationProperties.TryGetValue(key, out var value)
                    ? new[] { value.ToString()! }
                    : Enumerable.Empty<string>());

        using var activity = _activitySource.StartActivity(
            $"{message.Subject} consume",
            ActivityKind.Consumer,
            parentContext.ActivityContext);

        var body = Encoding.UTF8.GetString(message.Body);

        try
        {
            using var scope = serviceProvider.CreateScope();

            var integrationEvent = (IntegrationEvent?)JsonSerializer.Deserialize(
                body,
                eventType,
                _eventBusSubscriptionInfo.JsonSerializerOptions);

            if (integrationEvent is null)
            {
                throw new InvalidOperationException("Deserialized event is null.");
            }

            SetActivityContext(activity, args.EntityPath, "consume");
            var handler = scope.ServiceProvider.GetRequiredKeyedService<IIntegrationEventHandler>(handlerKey);

            await handler.HandleAsync(integrationEvent);

            await args.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            activity?.SetExceptionTags(ex);

            logger.LogError(
                ex,
                "Error processing Service Bus message {MessageId}",
                message.MessageId);

            await args.AbandonMessageAsync(message);
        }
    }

    private Task OnProcessError(ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "ServiceBus error. Entity: {EntityPath}, ErrorSource: {ErrorSource}",
            args.EntityPath,
            args.ErrorSource);

        return Task.CompletedTask;
    }

    private static void SetActivityContext(Activity? activity, string routingKey, string operation, string destinationKind = "queue")
    {
        if (activity is not null)
        {
            activity.SetTag("messaging.system", "servicebus");
            activity.SetTag("messaging.destination_kind", destinationKind);
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
        foreach (var processor in _processors)
        {
            await processor.StopProcessingAsync(cancellationToken); await processor.DisposeAsync();
        }
        _processors.Clear();
    }
}
