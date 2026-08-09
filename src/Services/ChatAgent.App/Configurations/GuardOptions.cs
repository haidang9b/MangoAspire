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

    /// <summary>Messages of prior conversation given to the relevance guard for context.</summary>
    public int HistoryLookback { get; set; } = 4;

    /// <summary>
    /// Master switch for the deterministic guard layers - prompt format checks, the injection
    /// scanner, and the answer fact checker.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <see cref="FailOpen"/>, and an incident kill-switch rather than
    /// a failure path. <c>FailOpen</c> exists because a model call can be unavailable; a regex
    /// cannot be. Routing the deterministic layers through it would make the strongest checks in
    /// the stack disappear during exactly the outage that removes the others.
    /// </remarks>
    public bool DeterministicEnabled { get; set; } = true;

    /// <summary>Longest customer message accepted, in characters.</summary>
    public int MaxPromptChars { get; set; } = 2000;

    /// <summary>Most lines accepted in a customer message.</summary>
    public int MaxPromptLines { get; set; } = 40;

    /// <summary>
    /// Longest message for which a lexicon hit may skip the LLM classifier.
    /// </summary>
    /// <remarks>
    /// The lexicon is broad on purpose - it includes every word of every product name, plus terms
    /// as common as "open" and "table" - because a false "on topic" only means the agent runs as
    /// it would have anyway. That reasoning holds for the off-topic axis and fails badly for the
    /// injection axis, since skipping the classifier skips the only check that owns it. Bounding
    /// the short-circuit to short messages keeps the cost saving for real questions while making
    /// a padded injection payload always reach the classifier.
    /// </remarks>
    public int LexiconMaxChars { get; set; } = 160;

    /// <summary>Cap applied to a chat message before it is persisted. Matches the column width.</summary>
    public int MaxStoredMessageChars { get; set; } = 4000;

    /// <summary>
    /// When a deterministic finding and the LLM reviewer disagree, the deterministic result wins.
    /// </summary>
    /// <remarks>
    /// The reviewer is shown retrieved facts that may themselves carry an injection; the fact
    /// checker is not persuadable. Turn this off only to work around a false positive in an
    /// incident.
    /// </remarks>
    public bool DeterministicOverridesReviewer { get; set; } = true;

    /// <summary>
    /// Whether the agent may answer availability questions at all.
    /// </summary>
    /// <remarks>
    /// True now that <c>available_stock</c> is replicated over CDC. This is only a master switch:
    /// with it on, an availability claim still has to be backed by a stock value that a tool
    /// actually returned, so a product whose stock has never replicated (null, not zero) still
    /// cannot be described as available or unavailable. Set it false to withdraw availability
    /// answers entirely - during a CDC outage, for instance, when replicated stock is stale.
    /// </remarks>
    public bool StockClaimsAllowed { get; set; } = true;

    /// <summary>Per-call timeout for a guard model request.</summary>
    public int GuardTimeoutSeconds { get; set; } = 15;

    /// <summary>Timeout for the agent's draft, including all of its tool round-trips.</summary>
    public int DraftTimeoutSeconds { get; set; } = 45;

    /// <summary>Whole-turn budget: draft plus review.</summary>
    public int TurnTimeoutSeconds { get; set; } = 60;

    public string MalformedMessage { get; set; } =
        "That message was too long or had characters I couldn't read - could you shorten it and try again?";

    public string TimeoutMessage { get; set; } =
        "Sorry, that took too long on my side. Could you try again?";

    public string OffTopicMessage { get; set; } =
        "I can only help with Mango Restaurant — our menu, your order, and store info like hours or delivery. What can I get you? 🍜";

    public string BlockedMessage { get; set; } =
        "Sorry, I can't help with that. Ask me about our dishes, your cart, or your order!";

    public string UnverifiedMessage { get; set; } =
        "Sorry — I couldn't put together a reliable answer for that. Could you rephrase, or ask about a specific dish?";
}
