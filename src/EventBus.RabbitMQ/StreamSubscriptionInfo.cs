namespace EventBus.RabbitMQ;

/// <summary>
/// Event types bound to replayable stream logs, populated by
/// <see cref="RabbitMQDependencyInjectionExtensions.AddStreamSubscription{T, TH}"/>.
/// </summary>
/// <remarks>
/// The stream counterpart of <see cref="RabbitMQInfo"/>. It records a <em>queue</em> to consume,
/// not an exchange to bind: the stream and its bindings are declared from the broker's
/// definitions.json at boot, so the topology outlives every service and no message is lost
/// before a consumer exists.
/// </remarks>
public class StreamSubscriptionInfo
{
    /// <summary>(stream queue name, event type) pairs.</summary>
    public List<(string StreamName, Type EventType)> EventTypes { get; } = [];
}

/// <summary>Tuning for the stream consume loop, bound from the <c>EventBus:Stream</c> section.</summary>
public class StreamConsumerOptions
{
    /// <summary>
    /// Unacked-message window. Streams require a non-zero prefetch — the broker refuses
    /// <c>basic.consume</c> without one — unlike classic queues, which this codebase leaves unset.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 100;

    /// <summary>
    /// How many messages may be processed before the offset is checkpointed. Lower means less
    /// reprocessing after a crash; higher means fewer writes. Re-processing is harmless because
    /// handlers fence on the source LSN.
    /// </summary>
    public int CheckpointEveryMessages { get; set; } = 50;

    /// <summary>
    /// Time-based checkpoint, so a stream that goes quiet still persists its position rather
    /// than leaving the last few messages to be replayed on the next restart.
    /// </summary>
    public TimeSpan CheckpointInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Handler attempts before a record is dead-lettered and the reader moves past it.
    /// A stream has no redelivery — an ack only advances the reader — so a transient failure
    /// has to be retried in process or the record is gone from the read-model for good.
    /// </summary>
    public int HandlerRetryCount { get; set; } = 5;
}
