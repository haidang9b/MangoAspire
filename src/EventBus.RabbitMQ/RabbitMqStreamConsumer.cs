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

/// <summary>
/// Reads Debezium change records from a RabbitMQ <b>stream</b> — an append-only, retained,
/// totally ordered log — instead of a classic queue.
/// </summary>
/// <remarks>
/// This exists to fix a specific failure: with classic queues, a message published before a
/// consumer's queue binding existed is discarded, so a service introduced later starts with a
/// permanently empty read-model and there is no way to ask for the history back.
/// <para>
/// A stream is read non-destructively — acking advances this consumer's position and nothing
/// else — so every service reads the whole log at its own pace from its own stored offset.
/// A service with no stored offset starts at <c>first</c> and replays everything retained,
/// which is exactly what onboarding a new service needs.
/// </para>
/// <para>
/// Kept separate from <see cref="RabbitMQEventBus"/> rather than folded into it, because the two
/// consume models genuinely differ: a stream requires a non-zero prefetch, rejects
/// <c>x-dead-letter-exchange</c>, and has no redelivery — so failures must be retried in process
/// rather than nacked.
/// </para>
/// </remarks>
public sealed class RabbitMqStreamConsumer(
    ILogger<RabbitMqStreamConsumer> logger,
    IServiceProvider serviceProvider,
    IOptions<EventBusOptions> options,
    IOptions<StreamConsumerOptions> streamOptions,
    IOptions<StreamSubscriptionInfo> subscriptionInfo,
    IOptions<EventBusSubscriptionInfo> serializationOptions,
    RabbitMQTelemetry telemetry)
    : IHostedService, IDisposable
{
    // Captured as fields because the nested StreamReader reads them; primary-constructor
    // parameters are not accessible from a nested type.
    private readonly ILogger<RabbitMqStreamConsumer> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private readonly StreamSubscriptionInfo _subscriptions = subscriptionInfo.Value;
    private readonly StreamConsumerOptions _streamOptions = streamOptions.Value;
    private readonly EventBusSubscriptionInfo _serialization = serializationOptions.Value;
    private readonly ActivitySource _activitySource = telemetry.ActivitySource;
    private readonly TextMapPropagator _propagator = telemetry.Propagator;
    private readonly string _dlxExchangeName = $"{options.Value.DomainName}.dlx";
    private readonly string _dlqQueueName = $"{options.Value.SubscriptionClientName}.dlx";

    private readonly List<StreamReader> _readers = [];
    private readonly CancellationTokenSource _stopping = new();
    private bool _disposed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_subscriptions.EventTypes.Count == 0)
        {
            return Task.CompletedTask;
        }

        // Same shape as RabbitMQEventBus.StartAsync: connecting must not block host startup,
        // because the broker may still be coming up.
        _ = Task.Factory.StartNew(
            async () =>
            {
                try
                {
                    await RunAsync(_stopping.Token);
                }
                catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
                {
                    // Shutting down.
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting the RabbitMQ stream consumer");
                }
            },
            TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var connection = _serviceProvider.GetRequiredService<IConnection>();

        await WaitForConnectionAsync(connection, cancellationToken);

        // One reader per stream, each with its own channel, offset and ordering guarantee.
        foreach (var streamName in _subscriptions.EventTypes.Select(x => x.StreamName).Distinct())
        {
            var reader = new StreamReader(streamName, this);
            _readers.Add(reader);
            await reader.StartAsync(connection, cancellationToken);
        }
    }

    private async Task WaitForConnectionAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>().Handle<SocketException>(),
                MaxRetryAttempts = int.MaxValue,
                DelayGenerator = context => ValueTask.FromResult(
                    (TimeSpan?)TimeSpan.FromSeconds(Math.Min(Math.Pow(2, context.AttemptNumber), 30))),
                OnRetry = args =>
                {
                    _logger.LogWarning(args.Outcome.Exception, "Could not connect to RabbitMQ, retrying in {TimeOut}s", args.RetryDelay.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        await pipeline.ExecuteAsync(async ct =>
        {
            if (!connection.IsOpen)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }
            await Task.CompletedTask;
        }, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _stopping.CancelAsync();

        // Flush positions so a clean shutdown does not replay the last batch on next start.
        foreach (var reader in _readers)
        {
            await reader.StopAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var reader in _readers)
        {
            reader.Dispose();
        }

        _stopping.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Consumes one stream: resolves the starting offset, processes records strictly in order,
    /// and checkpoints its position.
    /// </summary>
    private sealed class StreamReader(string streamName, RabbitMqStreamConsumer parent) : IDisposable
    {
        private readonly ResiliencePipeline _handlerPipeline = BuildHandlerPipeline(parent._streamOptions.HandlerRetryCount);

        // A stream's value is its ordering, so records are processed one at a time regardless of
        // how the client dispatches deliveries.
        private readonly SemaphoreSlim _processingGate = new(1, 1);

        private IChannel? _channel;
        private long _lastProcessedOffset = -1;
        private long _lastCheckpointedOffset = -1;
        private int _sinceCheckpoint;
        private DateTimeOffset _lastCheckpointAt = DateTimeOffset.MinValue;

        public async Task StartAsync(IConnection connection, CancellationToken cancellationToken)
        {
            var startOffset = await ResolveStartOffsetAsync(cancellationToken);

            _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            _channel.CallbackExceptionAsync += (_, ea) =>
            {
                parent._logger.LogWarning(ea.Exception, "Error on the {StreamName} consumer channel", streamName);
                return Task.CompletedTask;
            };

            await EnsureDeadLetterTopologyAsync(_channel, cancellationToken);

            // Streams require an explicit prefetch; without it the broker rejects basic.consume.
            await _channel.BasicQosAsync(0, parent._streamOptions.PrefetchCount, global: false, cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            await _channel.BasicConsumeAsync(
                queue: streamName,
                autoAck: false,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: new Dictionary<string, object?> { ["x-stream-offset"] = startOffset },
                consumer: consumer,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Resumes just past the last processed record, or replays the whole retained log when
        /// this service has never read the stream.
        /// </summary>
        private async Task<object> ResolveStartOffsetAsync(CancellationToken cancellationToken)
        {
            await using var scope = parent._serviceProvider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<ICdcOffsetStore>();
            var stored = await store.GetAsync(streamName, cancellationToken);

            if (stored is long offset)
            {
                _lastProcessedOffset = offset;
                _lastCheckpointedOffset = offset;
                parent._logger.LogInformation(
                    "Resuming stream {StreamName} at offset {Offset}", streamName, offset + 1);
                return offset + 1;
            }

            parent._logger.LogInformation(
                "No stored offset for stream {StreamName}; replaying the log from the beginning to rebuild the read-model",
                streamName);
            return "first";
        }

        /// <summary>
        /// Declares the dead-letter exchange and queue this reader falls back to. The stream
        /// itself cannot carry <c>x-dead-letter-exchange</c>, so poison records are republished
        /// explicitly. Declares match <see cref="RabbitMQEventBus"/>'s, so either may run first.
        /// </summary>
        private async Task EnsureDeadLetterTopologyAsync(IChannel channel, CancellationToken cancellationToken)
        {
            await channel.ExchangeDeclareAsync(
                exchange: parent._dlxExchangeName,
                type: ExchangeType.Fanout,
                cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: parent._dlqQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: parent._dlqQueueName,
                exchange: parent._dlxExchangeName,
                routingKey: string.Empty,
                cancellationToken: cancellationToken);
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
        {
            await _processingGate.WaitAsync();
            try
            {
                await ProcessDeliveryAsync(eventArgs);
            }
            finally
            {
                _processingGate.Release();
            }
        }

        private async Task ProcessDeliveryAsync(BasicDeliverEventArgs eventArgs)
        {
            var offset = ReadStreamOffset(eventArgs);
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

            var parentContext = parent._propagator.Extract(default, eventArgs.BasicProperties, ExtractTraceContext);
            Baggage.Current = parentContext.Baggage;

            using var activity = parent._activitySource.StartActivity(
                $"{eventName} receive", ActivityKind.Consumer, parentContext.ActivityContext);

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.operation", "receive");
            activity?.SetTag("messaging.destination.name", streamName);
            activity?.SetTag("messaging.rabbitmq.routing_key", eventName);
            if (offset is long o)
            {
                activity?.SetTag("messaging.rabbitmq.stream_offset", o);
            }

            try
            {
                // Retry in process: a stream has no redelivery, so giving up on the first
                // transient failure would drop the record from the read-model permanently.
                await _handlerPipeline.ExecuteAsync(async _ => await DispatchAsync(eventName, message));
            }
            catch (Exception ex)
            {
                activity?.SetExceptionTags(ex);
                parent._logger.LogError(
                    ex,
                    "Giving up on {EventName} at offset {Offset} on stream {StreamName} after {RetryCount} attempts; dead-lettering and advancing. The read-model is now missing this change — replay after fixing the cause.",
                    eventName, offset, streamName, parent._streamOptions.HandlerRetryCount);

                await DeadLetterAsync(eventArgs, ex);
            }

            await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);

            if (offset is long processed)
            {
                _lastProcessedOffset = processed;
                await MaybeCheckpointAsync();
            }
        }

        private async Task DispatchAsync(string eventName, string message)
        {
            await using var scope = parent._serviceProvider.CreateAsyncScope();

            var eventType = parent._subscriptions.EventTypes
                .Where(x => x.StreamName == streamName)
                .Select(x => x.EventType)
                .FirstOrDefault(type => EventRoutingKey.For(type) == eventName);

            if (eventType is null)
            {
                // Another consumer's routing key sharing the log. Not an error — ack and move on.
                parent._logger.LogTrace(
                    "No subscription on stream {StreamName} for routing key {EventName}; skipping",
                    streamName, eventName);
                return;
            }

            if (Deserialize(message, eventType, parent._serialization.JsonSerializerOptions) is not { } integrationEvent)
            {
                parent._logger.LogWarning("Unable to deserialize message to event type {EventType}", eventType.Name);
                return;
            }

            var handler = scope.ServiceProvider.GetRequiredKeyedService<IIntegrationEventHandler>(eventType);
            await handler.HandleAsync(integrationEvent);
        }

        /// <summary>
        /// Persists the position once enough records or enough time have passed.
        /// </summary>
        /// <remarks>
        /// Deliberately outside the handler's transaction. A crash in between replays the last
        /// few records, which the handlers' LSN fence turns into no-ops — whereas sharing a
        /// transaction would drag the consuming service's DbContext into this library.
        /// </remarks>
        private async Task MaybeCheckpointAsync()
        {
            _sinceCheckpoint++;

            var dueByCount = _sinceCheckpoint >= parent._streamOptions.CheckpointEveryMessages;
            var dueByTime = DateTimeOffset.UtcNow - _lastCheckpointAt >= parent._streamOptions.CheckpointInterval;

            if (!dueByCount && !dueByTime)
            {
                return;
            }

            await CheckpointAsync(CancellationToken.None);
        }

        private async Task CheckpointAsync(CancellationToken cancellationToken)
        {
            if (_lastProcessedOffset < 0 || _lastProcessedOffset == _lastCheckpointedOffset)
            {
                return;
            }

            try
            {
                await using var scope = parent._serviceProvider.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<ICdcOffsetStore>();
                await store.SaveAsync(streamName, _lastProcessedOffset, cancellationToken);

                _lastCheckpointedOffset = _lastProcessedOffset;
                _sinceCheckpoint = 0;
                _lastCheckpointAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                // Non-fatal: the position is simply re-read next time, and reprocessing is safe.
                parent._logger.LogWarning(
                    ex, "Could not checkpoint offset {Offset} for stream {StreamName}", _lastProcessedOffset, streamName);
            }
        }

        private async Task DeadLetterAsync(BasicDeliverEventArgs eventArgs, Exception ex)
        {
            try
            {
                var properties = new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent,
                    Headers = new Dictionary<string, object?>()
                };

                if (eventArgs.BasicProperties.Headers is { } headers)
                {
                    foreach (var header in headers)
                    {
                        properties.Headers[header.Key] = header.Value;
                    }
                }

                properties.Headers["x-exception-message"] = ex.Message;
                properties.Headers["x-exception-stacktrace"] = ex.ToString();
                properties.Headers["x-original-routing-key"] = eventArgs.RoutingKey;
                properties.Headers["x-original-stream"] = streamName;

                await _channel!.BasicPublishAsync(
                    exchange: parent._dlxExchangeName,
                    routingKey: eventArgs.RoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: eventArgs.Body);
            }
            catch (Exception publishEx)
            {
                parent._logger.LogError(publishEx, "Could not dead-letter a failed record from stream {StreamName}", streamName);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await CheckpointAsync(cancellationToken);

            if (_channel is not null)
            {
                await _channel.CloseAsync(cancellationToken);
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _processingGate.Dispose();
        }

        /// <summary>
        /// The stream position of this delivery, which the broker attaches as a header. It is
        /// the only thing that makes resuming (and therefore replay) possible.
        /// </summary>
        private static long? ReadStreamOffset(BasicDeliverEventArgs eventArgs)
            => eventArgs.BasicProperties.Headers?.TryGetValue("x-stream-offset", out var raw) == true
                ? raw switch
                {
                    long l => l,
                    int i => i,
                    ulong ul => (long)ul,
                    uint ui => ui,
                    _ => null
                }
                : null;

        private static IEnumerable<string> ExtractTraceContext(IReadOnlyBasicProperties props, string key)
            => props.Headers is not null && props.Headers.TryGetValue(key, out var value) && value is byte[] bytes
                ? [Encoding.UTF8.GetString(bytes)]
                : [];

        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
            Justification = "Matches RabbitMQEventBus: the JsonSerializer.IsReflectionEnabledByDefault feature switch guards reflection use.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "See above.")]
        private static IntegrationEvent? Deserialize(string message, Type eventType, JsonSerializerOptions options)
            => JsonSerializer.Deserialize(message, eventType, options) as IntegrationEvent;

        private static ResiliencePipeline BuildHandlerPipeline(int retryCount)
            => new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    MaxRetryAttempts = retryCount,
                    DelayGenerator = context => ValueTask.FromResult(
                        (TimeSpan?)TimeSpan.FromSeconds(Math.Min(Math.Pow(2, context.AttemptNumber), 30)))
                })
                .Build();
    }
}
