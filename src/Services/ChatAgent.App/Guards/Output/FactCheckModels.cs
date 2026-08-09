using System.Text;

namespace ChatAgent.App.Guards.Output;

/// <param name="RuleId">Stable identifier for logs and metrics.</param>
/// <param name="Claim">The exact text from the draft that failed.</param>
/// <param name="Detail">Why it failed, phrased for the LLM reviewer's prompt.</param>
/// <param name="IsHard">
/// True when the finding cannot be revised away - a leaked identifier is not a phrasing problem,
/// so there is nothing for a reviewer to salvage and no reason to spend a model call finding out.
/// </param>
public record FactCheckIssue(string RuleId, string Claim, string Detail, bool IsHard);

/// <param name="Issues">Every finding, in rule order. Empty when the draft checks out.</param>
public record FactCheckResult(IReadOnlyList<FactCheckIssue> Issues)
{
    public static readonly FactCheckResult Pass = new([]);

    public bool Passed => Issues.Count == 0;

    public bool HasHardViolation => Issues.Any(i => i.IsHard);

    public IReadOnlyList<string> RuleIds => [.. Issues.Select(i => i.RuleId).Distinct()];

    /// <summary>
    /// Renders the findings for the reviewer's prompt. Claims are quoted from the draft, which the
    /// agent wrote, so this is not a place untrusted text enters the prompt.
    /// </summary>
    public string ToPromptText()
    {
        if (Issues.Count == 0)
        {
            return "(none)";
        }

        var builder = new StringBuilder();
        foreach (var issue in Issues)
        {
            builder.Append("- \"").Append(issue.Claim).Append("\" - ").AppendLine(issue.Detail);
        }

        return builder.ToString();
    }
}
