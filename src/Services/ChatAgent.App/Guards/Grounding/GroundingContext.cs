using ChatAgent.App.Guards.Untrusted;
using System.Text;

namespace ChatAgent.App.Guards.Grounding;

/// <param name="ToolName">Kernel function that produced the result.</param>
/// <param name="Result">
/// Serialised tool output, neutralised and truncated. Untrusted: a tool result is built from
/// database text, knowledge base documents and web pages, none of which this service authored.
/// </param>
public record GroundingEntry(string ToolName, string Result);

/// <param name="Entries">Every tool result captured during the turn, in invocation order.</param>
/// <param name="Truncated">True when some output was cut to fit the guard's budget.</param>
public record GroundingSnapshot(IReadOnlyList<GroundingEntry> Entries, bool Truncated)
{
    public static readonly GroundingSnapshot Empty = new([], false);

    public bool HasFacts => Entries.Count > 0;

    /// <summary>
    /// Renders the captured facts for the response guard's prompt, each entry inside
    /// <paramref name="fence"/> so the guard cannot mistake tool output for its own instructions.
    /// </summary>
    /// <remarks>
    /// This used to emit a bare <c>### {toolName}</c> heading per entry, which meant a product
    /// description containing <c>### GetAllProductsAsync</c> forged a tool-result section inside
    /// the prompt of the guard meant to catch it. The tool name is prompt structure and stays
    /// outside the fence; everything the tool returned goes inside it.
    /// </remarks>
    public string ToPromptText(IUntrustedFence fence)
    {
        if (Entries.Count == 0)
        {
            return "(no tools were called - the assistant had no retrieved facts to work from)";
        }

        var builder = new StringBuilder();
        foreach (var entry in Entries)
        {
            builder.AppendLine(fence.Wrap(entry.ToolName, entry.Result));
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

    private readonly Lock _gate = new();
    private readonly List<GroundingEntry> _entries = [];
    private bool _truncated;

    public void Record(string toolName, string? result)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        // Neutralised on the way in rather than on the way out, so there is no path by which the
        // response guard can be handed raw tool output - including from a future caller that
        // renders a snapshot without going through ToPromptText.
        result = UntrustedText.Neutralize(result);

        if (result.Length == 0)
        {
            return;
        }

        var truncated = false;
        if (result.Length > MaxResultChars)
        {
            result = result[..MaxResultChars];
            truncated = true;
        }

        // Semantic Kernel can invoke several tools concurrently within one round-trip, so the
        // list and the flag are both written from more than one thread.
        lock (_gate)
        {
            _entries.Add(new GroundingEntry(toolName, result));
            _truncated |= truncated;
        }
    }

    public GroundingSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new GroundingSnapshot([.. _entries], _truncated);
        }
    }
}
