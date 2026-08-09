using System.Text.RegularExpressions;

namespace ChatAgent.App.Guards.Output;

/// <summary>
/// Verifies that a guard's revision only ever <em>removed</em> words from the draft.
/// </summary>
/// <remarks>
/// <para>
/// The response guard is shown retrieved facts, and retrieved facts are untrusted. On the revise
/// path that same guard authors the text the customer reads - so a reviewer able to <em>add</em>
/// words is a route by which untrusted content reaches the customer in the assistant's voice,
/// having passed every check upstream of it.
/// </para>
/// <para>
/// Requiring the revision to be a word-level subsequence of the draft closes that route by
/// construction: the guard can delete an unsupported claim, but cannot introduce a single word the
/// agent did not already produce. A length cap would only bound how much injected text got
/// through, not whether any did.
/// </para>
/// <para>
/// The cost is real and worth naming: a revision that needs rewording rather than cutting
/// ("we open at 9" to "we open at 10") cannot be expressed, and degrades to a shorter answer or to
/// a rejection. That is the right way round - correcting a number is precisely the case where the
/// reviewer would be asserting a fact of its own.
/// </para>
/// </remarks>
public static partial class RevisionValidator
{
    /// <summary>
    /// True when every word of <paramref name="revision"/> appears in <paramref name="draft"/>, in
    /// the same order.
    /// </summary>
    public static bool IsDeletionOnly(string? draft, string? revision)
    {
        if (string.IsNullOrWhiteSpace(revision))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(draft))
        {
            return false;
        }

        var draftWords = Tokenize(draft);
        var revisionWords = Tokenize(revision);

        if (revisionWords.Count > draftWords.Count)
        {
            return false;
        }

        var draftIndex = 0;
        foreach (var word in revisionWords)
        {
            var found = false;
            while (draftIndex < draftWords.Count)
            {
                if (string.Equals(draftWords[draftIndex++], word, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Splits on whitespace and strips surrounding punctuation.
    /// </summary>
    /// <remarks>
    /// Punctuation is ignored on purpose: joining two sentences after cutting the middle one
    /// legitimately changes a comma to a full stop, and failing the revision for that would push
    /// every real edit into a rejection. Words are what carry the claims.
    /// </remarks>
    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();

        foreach (var raw in WhitespaceRegex().Split(text))
        {
            var token = raw.Trim(Punctuation);
            if (token.Length > 0)
            {
                tokens.Add(token);
            }
        }

        return tokens;
    }

    private static readonly char[] Punctuation =
        ['.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '*', '_', '-'];

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
