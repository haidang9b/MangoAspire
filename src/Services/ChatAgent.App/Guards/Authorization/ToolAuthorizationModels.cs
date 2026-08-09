namespace ChatAgent.App.Guards.Authorization;

/// <param name="RuleId">Stable identifier for logs and metrics; empty when allowed.</param>
/// <param name="Reason">
/// Why the call was refused. Returned to the model so it can tell the customer something useful,
/// so it must describe the rule and never the internal state that failed it.
/// </param>
public record ToolAuthorizationDecision(bool Allowed, string RuleId, string Reason)
{
    public static readonly ToolAuthorizationDecision Allow = new(true, string.Empty, string.Empty);

    public static ToolAuthorizationDecision Deny(string ruleId, string reason) => new(false, ruleId, reason);
}
