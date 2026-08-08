namespace ChatAgent.App.Configurations;

/// <summary>
/// Settings for the two customer-facing guardrails: a relevance check before the agent
/// runs, and a verification pass over the drafted answer before it is streamed out.
/// </summary>
public class GuardOptions
{
    /// <summary>Quick guard: reject questions that have nothing to do with the shop.</summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary>Response guard: verify the draft answer before the customer sees it.</summary>
    public bool OutputEnabled { get; set; } = true;

    /// <summary>
    /// Deployment used for guard calls. Falls back to the main chat model when blank; set
    /// it to a cheaper deployment to cut guard cost.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// What to do when a guard itself errors. Open (default) means a transient Azure
    /// failure degrades the guard rather than taking the whole chat down; closed means an
    /// unverifiable answer is never shown.
    /// </summary>
    public bool FailOpen { get; set; } = true;

    /// <summary>
    /// Cap on auto-invoked tool round-trips per turn. Semantic Kernel does not bound this
    /// on its own, so a confused model can otherwise loop indefinitely.
    /// </summary>
    public int MaxToolIterations { get; set; } = 6;

    /// <summary>Characters of prior conversation given to the relevance guard for context.</summary>
    public int HistoryLookback { get; set; } = 4;

    public string OffTopicMessage { get; set; } =
        "I can only help with Mango Restaurant — our menu, your order, and store info like hours or delivery. What can I get you? 🍜";

    public string BlockedMessage { get; set; } =
        "Sorry, I can't help with that. Ask me about our dishes, your cart, or your order!";

    public string UnverifiedMessage { get; set; } =
        "Sorry — I couldn't put together a reliable answer for that. Could you rephrase, or ask about a specific dish?";
}
