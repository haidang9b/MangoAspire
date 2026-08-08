using System.Text;

namespace ChatAgent.App.Guards.Grounding;

/// <param name="ToolName">Kernel function that produced the result.</param>
/// <param name="Result">Serialised tool output, truncated to keep the guard prompt bounded.</param>
public record GroundingEntry(string ToolName, string Result);

/// <param name="Entries">Every tool result captured during the turn, in invocation order.</param>
/// <param name="Truncated">True when some output was cut to fit the guard's budget.</param>
public record GroundingSnapshot(IReadOnlyList<GroundingEntry> Entries, bool Truncated)
{
    public static readonly GroundingSnapshot Empty = new([], false);

    public bool HasFacts => Entries.Count > 0;

    /// <summary>Renders the captured facts for inclusion in the response guard's prompt.</summary>
    public string ToPromptText()
    {
        if (Entries.Count == 0)
        {
            return "(no tools were called — the assistant had no retrieved facts to work from)";
        }

        var builder = new StringBuilder();
        foreach (var entry in Entries)
        {
            builder.Append("### ").AppendLine(entry.ToolName);
            builder.AppendLine(entry.Result);
            builder.AppendLine();
        }

        if (Truncated)
        {
            builder.AppendLine("(some tool output was truncated)");
        }

        return builder.ToString();
    }
}

/// <summary>
/// Per-request store of what the tools actually returned. Scoped, so one customer's facts
/// can never leak into another's verification.
/// </summary>
public interface IGroundingContext
{
    void Record(string toolName, string? result);

    GroundingSnapshot Snapshot();
}

/// <inheritdoc cref="IGroundingContext"/>
public class GroundingContext : IGroundingContext
{
    /// <summary>
    /// Per-tool cap on captured output. The guard prompt has to stay small enough to be
    /// cheap, and a full menu dump adds no verification value over the first few entries.
    /// </summary>
    private const int MaxResultChars = 4000;

    private readonly List<GroundingEntry> _entries = [];
    private bool _truncated;

    public void Record(string toolName, string? result)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        if (result.Length > MaxResultChars)
        {
            result = result[..MaxResultChars];
            _truncated = true;
        }

        _entries.Add(new GroundingEntry(toolName, result));
    }

    public GroundingSnapshot Snapshot() => new([.. _entries], _truncated);
}
