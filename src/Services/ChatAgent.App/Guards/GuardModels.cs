namespace ChatAgent.App.Guards;

/// <summary>Why the relevance guard allowed or blocked a question.</summary>
public enum GuardCategory
{
    /// <summary>About the restaurant, its menu, or the customer's order.</summary>
    OnTopic = 0,

    /// <summary>A legitimate question, but nothing to do with the shop.</summary>
    OffTopic = 1,

    /// <summary>An attempt to override the agent's instructions or extract them.</summary>
    PromptInjection = 2,

    /// <summary>Harmful, abusive, or otherwise inappropriate for a customer channel.</summary>
    Unsafe = 3,
}

/// <param name="Allowed">False means the agent is never invoked for this turn.</param>
/// <param name="Reason">Short rationale, for logs only — never shown to the customer.</param>
public record GuardVerdict(bool Allowed, GuardCategory Category, string? Reason = null)
{
    public static GuardVerdict Allow(string? reason = null)
        => new(true, GuardCategory.OnTopic, reason);

    public static GuardVerdict Block(GuardCategory category, string? reason = null)
        => new(false, category, reason);
}

/// <summary>Outcome of verifying a drafted answer.</summary>
public enum ReviewVerdictKind
{
    /// <summary>The draft is grounded and safe; send it as-is.</summary>
    Approved = 0,

    /// <summary>The draft had a problem the guard could correct; send the rewrite.</summary>
    Revised = 1,

    /// <summary>The draft could not be salvaged; send the configured fallback.</summary>
    Rejected = 2,
}

/// <param name="Content">The text to actually send to the customer.</param>
/// <param name="Reason">Short rationale, for logs only — never shown to the customer.</param>
public record ReviewVerdict(ReviewVerdictKind Kind, string Content, string? Reason = null)
{
    public static ReviewVerdict Approve(string content, string? reason = null)
        => new(ReviewVerdictKind.Approved, content, reason);
}
