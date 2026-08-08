using EventBus.Events;
using System.Text.Json.Serialization;

namespace ShoppingCart.API.Cdc;

/// <summary>
/// Shared shape of every Debezium change record: the delete marker plus the source metadata
/// used to order events.
/// </summary>
/// <remarks>
/// The metadata fields come from <c>ExtractNewRecordState</c>'s <c>add.fields</c> setting in
/// <c>init-configs/products/application.properties</c>, which injects them into the payload
/// rather than only the AMQP headers — so ordering survives deserialization and is testable.
/// <para>
/// They matter because the CDC log is replayable: a consumer rebuilding its read-model
/// re-reads old records, and without a fence those would overwrite newer state. Handlers
/// compare <see cref="SourceLsn"/> against the value stamped on the local row and skip
/// anything that is not strictly newer.
/// </para>
/// </remarks>
public abstract record CdcIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Postgres WAL position of the originating commit. Monotonic per cluster — including
    /// across replication-slot recreation — which makes it the primary ordering fence.
    /// Null on messages published before <c>add.fields</c> was configured.
    /// </summary>
    [JsonPropertyName("__source_lsn")]
    public long? SourceLsn { get; set; }

    /// <summary>Source commit time in Unix milliseconds; the fallback fence when no LSN is present.</summary>
    [JsonPropertyName("__source_ts_ms")]
    public long? SourceTimestampMs { get; set; }

    /// <summary>Debezium operation: <c>r</c> (snapshot read), <c>c</c>, <c>u</c> or <c>d</c>.</summary>
    [JsonPropertyName("__op")]
    public string? Op { get; set; }

    /// <summary>Source transaction id, for correlating changes committed together.</summary>
    [JsonPropertyName("__source_txId")]
    public long? SourceTransactionId { get; set; }

    /// <summary>
    /// Debezium's <c>delete.handling.mode=rewrite</c> marks deletes with this flag rather
    /// than emitting a tombstone, so it arrives as the string "true"/"false".
    /// </summary>
    [JsonPropertyName("__deleted")]
    public string? DeletedRaw { get; set; }

    [JsonIgnore]
    public bool IsDeleted => string.Equals(DeletedRaw, "true", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public DateTime? SourceTimestamp => SourceTimestampMs is long ms
        ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime
        : null;

    /// <summary>
    /// True when this record is older than what the local row already reflects, and applying
    /// it would move the read-model backwards.
    /// </summary>
    /// <remarks>
    /// Uses <c>&lt;=</c> on the LSN so an exact redelivery is a no-op: CDC is at-least-once and
    /// replays are routine. When neither side carries metadata the event is applied, which
    /// keeps messages published before <c>add.fields</c> working.
    /// </remarks>
    public bool IsStaleAgainst(long? rowLsn, DateTime? rowTimestamp)
    {
        if (rowLsn is long currentLsn && SourceLsn is long incomingLsn)
        {
            return incomingLsn <= currentLsn;
        }

        if (rowTimestamp is DateTime currentTs && SourceTimestamp is DateTime incomingTs)
        {
            return incomingTs < currentTs;
        }

        return false;
    }
}
