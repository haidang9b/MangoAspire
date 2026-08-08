using System.Text;
using System.Text.RegularExpressions;

namespace ChatAgent.App.Services;

/// <inheritdoc cref="IMarkdownChunker"/>
/// <remarks>
/// <para>
/// Splitting on <c>##</c> alone is not enough for real documents: a long policy section
/// blows past the embedding model's input window, and a chunk that broad gets retrieved
/// for every question. So the splitter descends only as far as it must.
/// </para>
/// <list type="number">
/// <item><b>Structure</b> — parse the heading tree and treat each leaf section as a
/// candidate chunk carrying its full heading breadcrumb.</item>
/// <item><b>Descend</b> — a candidate over budget is re-split by the next separator in
/// priority order (blank-line paragraphs, then single lines, then sentences, then a hard
/// character cut), re-checking after each level. Descent stops as soon as the pieces fit,
/// so well-sized sections are never over-fragmented.</item>
/// <item><b>Merge</b> — adjacent runt pieces are coalesced back together while they stay
/// under budget, so a page of one-line bullets does not become hundreds of fragments.</item>
/// <item><b>Overlap</b> — trailing text from the previous piece is repeated at the start
/// of the next, within a section only, so a sentence spanning a boundary survives.</item>
/// <item><b>Breadcrumb</b> — every chunk is prefixed with its heading path. This is what
/// keeps a fragment like "within 5-7 business days" retrievable for "refund policy", and
/// it is why the sentence and hard-cut tiers stay usable instead of producing orphans.</item>
/// </list>
/// </remarks>
public partial class MarkdownChunker : IMarkdownChunker
{
    /// <summary>
    /// Floor on the per-chunk text budget, so a deep breadcrumb cannot starve the content
    /// down to nothing (or a negative budget) on a small MaxChunkChars.
    /// </summary>
    private const int MinimumContentBudget = 120;

    private const string BreadcrumbSeparator = " > ";

    public IReadOnlyList<MarkdownChunk> Chunk(string markdown, KnowledgeBaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var chunks = new List<MarkdownChunk>();
        var ordinal = 0;

        foreach (var section in ParseSections(markdown))
        {
            var prefix = string.IsNullOrEmpty(section.Breadcrumb)
                ? string.Empty
                : section.Breadcrumb + "\n\n";

            var budget = Math.Max(options.MaxChunkChars - prefix.Length, MinimumContentBudget);

            var pieces = SplitToBudget(SegmentBlocks(section.Body), budget, "\n\n");
            pieces = MergeSmallPieces(pieces, budget, options.MinChunkChars);
            pieces = ApplyOverlap(pieces, options.ChunkOverlapChars, budget);

            foreach (var piece in pieces)
            {
                chunks.Add(new MarkdownChunk(ordinal++, section.Breadcrumb, prefix + piece));
            }
        }

        return chunks;
    }

    // ---------------------------------------------------------------- structure

    private record Section(string Breadcrumb, string Body);

    /// <summary>
    /// Walks the document once, tracking the heading stack, and emits one section per
    /// heading that actually has body text. Headings inside fenced code blocks are ignored
    /// so a <c>#</c> comment in a shell sample does not fabricate a section.
    /// </summary>
    private static List<Section> ParseSections(string markdown)
    {
        var sections = new List<Section>();
        var headings = new List<(int Level, string Text)>();
        var body = new StringBuilder();
        var inFence = false;

        void Flush()
        {
            var text = body.ToString().Trim();
            if (text.Length > 0)
            {
                sections.Add(new Section(BuildBreadcrumb(headings), text));
            }

            body.Clear();
        }

        foreach (var line in markdown.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                body.AppendLine(line);
                continue;
            }

            var heading = inFence ? null : MatchHeading(line);
            if (heading is null)
            {
                body.AppendLine(line);
                continue;
            }

            Flush();

            var (level, text) = heading.Value;

            // Pop any heading at or below this level; what remains is the parent path.
            headings.RemoveAll(h => h.Level >= level);
            headings.Add((level, text));
        }

        Flush();

        return sections;
    }

    private static (int Level, string Text)? MatchHeading(string line)
    {
        var match = HeadingRegex().Match(line);
        return match.Success
            ? (match.Groups[1].Value.Length, match.Groups[2].Value.Trim())
            : null;
    }

    private static string BuildBreadcrumb(List<(int Level, string Text)> headings)
        => string.Join(BreadcrumbSeparator, headings.Select(h => h.Text));

    // ---------------------------------------------------------------- segmentation

    /// <summary>
    /// Breaks a section body into top-level blocks, keeping fenced code and contiguous
    /// table rows as single atomic units so packing never cuts through the middle of one.
    /// (An atomic block larger than the budget still has to be split — see
    /// <see cref="SplitOversized"/> — but it is preserved whenever it fits.)
    /// </summary>
    private static List<string> SegmentBlocks(string body)
    {
        var blocks = new List<string>();
        var current = new StringBuilder();
        var lines = body.ReplaceLineEndings("\n").Split('\n');
        var i = 0;

        void FlushCurrent()
        {
            var text = current.ToString().Trim();
            if (text.Length > 0)
            {
                blocks.Add(text);
            }

            current.Clear();
        }

        while (i < lines.Length)
        {
            var line = lines[i];

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                FlushCurrent();

                var fence = new StringBuilder(line);
                i++;
                while (i < lines.Length)
                {
                    fence.Append('\n').Append(lines[i]);
                    var closed = lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal);
                    i++;
                    if (closed)
                    {
                        break;
                    }
                }

                blocks.Add(fence.ToString());
                continue;
            }

            if (line.TrimStart().StartsWith('|'))
            {
                FlushCurrent();

                var table = new StringBuilder();
                while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
                {
                    if (table.Length > 0)
                    {
                        table.Append('\n');
                    }

                    table.Append(lines[i]);
                    i++;
                }

                blocks.Add(table.ToString());
                continue;
            }

            if (line.Trim().Length == 0)
            {
                FlushCurrent();
                i++;
                continue;
            }

            current.AppendLine(line);
            i++;
        }

        FlushCurrent();

        return blocks;
    }

    // ---------------------------------------------------------------- packing

    /// <summary>
    /// Greedily packs units into chunks up to <paramref name="budget"/>, recursing into any
    /// single unit that is too large on its own.
    /// </summary>
    private static List<string> SplitToBudget(List<string> units, int budget, string separator)
    {
        var result = new List<string>();
        var buffer = new StringBuilder();

        void FlushBuffer()
        {
            if (buffer.Length > 0)
            {
                result.Add(buffer.ToString());
                buffer.Clear();
            }
        }

        foreach (var unit in units)
        {
            if (unit.Length > budget)
            {
                FlushBuffer();
                result.AddRange(SplitOversized(unit, budget));
                continue;
            }

            var projected = buffer.Length == 0
                ? unit.Length
                : buffer.Length + separator.Length + unit.Length;

            if (projected > budget)
            {
                FlushBuffer();
            }

            if (buffer.Length > 0)
            {
                buffer.Append(separator);
            }

            buffer.Append(unit);
        }

        FlushBuffer();

        return result;
    }

    /// <summary>
    /// Descends through progressively finer separators until the pieces fit. Each level is
    /// only reached when the level above failed to make progress.
    /// </summary>
    private static List<string> SplitOversized(string text, int budget)
    {
        // Blank-line paragraphs.
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        if (paragraphs.Count > 1)
        {
            return SplitToBudget(paragraphs, budget, "\n\n");
        }

        // Individual lines — this is what degrades a giant table or code block gracefully,
        // one row or statement at a time.
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd())
            .Where(l => l.Trim().Length > 0)
            .ToList();

        if (lines.Count > 1)
        {
            return SplitToBudget(lines, budget, "\n");
        }

        // Sentences.
        var sentences = SentenceRegex().Split(text)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        if (sentences.Count > 1)
        {
            return SplitToBudget(sentences, budget, " ");
        }

        return HardSplit(text, budget);
    }

    /// <summary>
    /// Last resort for a single unbroken run longer than the budget: cut on the nearest
    /// preceding whitespace so words stay whole.
    /// </summary>
    private static List<string> HardSplit(string text, int budget)
    {
        var pieces = new List<string>();
        var position = 0;

        while (position < text.Length)
        {
            var length = Math.Min(budget, text.Length - position);

            if (position + length < text.Length)
            {
                var window = text.AsSpan(position, length);
                var lastSpace = window.LastIndexOf(' ');

                // Only honour the word boundary if it is not pathologically early,
                // otherwise a long unbroken token would make no progress.
                if (lastSpace > budget / 2)
                {
                    length = lastSpace;
                }
            }

            pieces.Add(text.Substring(position, length).Trim());
            position += length;
        }

        return [.. pieces.Where(p => p.Length > 0)];
    }

    // ---------------------------------------------------------------- post-processing

    /// <summary>
    /// Coalesces runt pieces into their neighbour where the result still fits, so a
    /// document of short bullets yields a few useful chunks rather than many weak ones.
    /// </summary>
    private static List<string> MergeSmallPieces(List<string> pieces, int budget, int minChunkChars)
    {
        if (pieces.Count <= 1 || minChunkChars <= 0)
        {
            return pieces;
        }

        var merged = new List<string>();

        foreach (var piece in pieces)
        {
            if (merged.Count == 0)
            {
                merged.Add(piece);
                continue;
            }

            var previous = merged[^1];
            var combinedLength = previous.Length + 2 + piece.Length;

            var eitherIsRunt = previous.Length < minChunkChars || piece.Length < minChunkChars;

            if (eitherIsRunt && combinedLength <= budget)
            {
                merged[^1] = previous + "\n\n" + piece;
            }
            else
            {
                merged.Add(piece);
            }
        }

        return merged;
    }

    /// <summary>
    /// Prepends the tail of each piece to the next so a fact split across a boundary is
    /// still retrievable from either side. Overlap is never applied across sections —
    /// callers invoke this per section — and is skipped when it would break the budget.
    /// </summary>
    private static List<string> ApplyOverlap(List<string> pieces, int overlapChars, int budget)
    {
        if (pieces.Count <= 1 || overlapChars <= 0)
        {
            return pieces;
        }

        var result = new List<string> { pieces[0] };

        for (var i = 1; i < pieces.Count; i++)
        {
            var previous = pieces[i - 1];
            var current = pieces[i];

            var tailLength = Math.Min(overlapChars, previous.Length);
            var tail = previous[^tailLength..];

            // Start the overlap at a word boundary so it reads as text, not a fragment.
            var space = tail.IndexOf(' ');
            if (space > 0 && space < tail.Length - 1)
            {
                tail = tail[(space + 1)..];
            }

            tail = tail.Trim();

            result.Add(tail.Length > 0 && current.Length + tail.Length + 2 <= budget
                ? tail + "\n\n" + current
                : current);
        }

        return result;
    }

    [GeneratedRegex(@"^(#{1,6})\s+(.*)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceRegex();
}
