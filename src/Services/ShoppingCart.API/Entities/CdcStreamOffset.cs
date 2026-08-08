namespace ShoppingCart.API.Entities;

/// <summary>
/// How far this service has read into a replayable CDC stream.
/// </summary>
/// <remarks>
/// The broker does not track this for us — a stream is read independently by every consumer —
/// so the position lives here, in the same database as the read-model it feeds.
/// <b>Deleting a row is the replay button:</b> with no stored offset the consumer restarts at
/// the beginning of the log and rebuilds the product mirror from scratch.
/// </remarks>
public class CdcStreamOffset
{
    /// <summary>The stream queue name, e.g. <c>mango.cdc.stream</c>.</summary>
    public required string StreamName { get; set; }

    /// <summary>Offset of the last record this service processed successfully.</summary>
    public long Offset { get; set; }

    public DateTime UpdatedAt { get; set; }
}
