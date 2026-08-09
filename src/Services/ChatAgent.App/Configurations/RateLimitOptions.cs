namespace ChatAgent.App.Configurations;

/// <summary>
/// Throttles the chat endpoint. One turn costs two to four model calls, so an unthrottled
/// endpoint converts a valid token into an unbounded bill.
/// </summary>
public class RateLimitOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Turns allowed per window, per customer.</summary>
    public int PermitLimit { get; set; } = 12;

    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Segments the window is divided into. More segments make the limit slide more smoothly
    /// instead of resetting in a burst on a window boundary.
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 6;

    /// <summary>
    /// Turns one customer may have in flight at once.
    /// </summary>
    /// <remarks>
    /// Separate from the window limit and arguably the more important of the two. A turn is
    /// buffered end to end before anything is streamed, so it holds a connection and an agent
    /// slot for tens of seconds; without a concurrency bound, one customer with several tabs open
    /// occupies the service while staying well inside the per-minute allowance.
    /// </remarks>
    public int ConcurrentTurns { get; set; } = 1;

    /// <summary>Value sent in the <c>Retry-After</c> header on a rejection.</summary>
    public int RetryAfterSeconds { get; set; } = 20;

    public string RejectionMessage { get; set; } =
        "You're sending messages a bit fast - give me a moment and try again.";
}
