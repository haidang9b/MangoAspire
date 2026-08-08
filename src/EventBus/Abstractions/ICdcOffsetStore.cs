namespace EventBus.Abstractions;

/// <summary>
/// Remembers how far a service has read into a replayable event log.
/// </summary>
/// <remarks>
/// A stream is an append-only log that every consumer reads independently, so "how far have I
/// got" is the consumer's own state, not the broker's. Each service persists it in its own
/// database, which makes replay an ordinary data operation: <b>delete the row and restart, and
/// the service rebuilds its read-model from the beginning of the log.</b>
/// <para>
/// Offsets are checkpointed outside the handler's transaction, so a crash between handling and
/// checkpointing replays the last few records — at-least-once. That is safe because CDC
/// handlers fence on the source LSN and re-applying a record they have already seen is a no-op.
/// </para>
/// <para>
/// Implementations live in the consuming service, which is what keeps EF Core out of
/// EventBus.RabbitMQ.
/// </para>
/// </remarks>
public interface ICdcOffsetStore
{
    /// <summary>
    /// The last offset successfully processed on <paramref name="streamName"/>, or
    /// <see langword="null"/> if this service has never read it — which means start at the
    /// beginning and replay everything the log still retains.
    /// </summary>
    Task<long?> GetAsync(string streamName, CancellationToken cancellationToken = default);

    /// <summary>Records <paramref name="offset"/> as the last successfully processed position.</summary>
    Task SaveAsync(string streamName, long offset, CancellationToken cancellationToken = default);
}
