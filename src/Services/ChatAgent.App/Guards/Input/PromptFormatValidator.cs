using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatAgent.App.Guards.Input;

/// <summary>
/// Structural validation of a customer prompt: size, shape, and character legality.
/// </summary>
/// <remarks>
/// Static and dependency-free on purpose. This is the one layer in the guard stack that cannot
/// fail for an external reason - no model, no network, no database - so it must not be reachable
/// from the fail-open path that exists for those. See <see cref="GuardOptions.FailOpen"/>.
/// </remarks>
public static partial class PromptFormatValidator
{
    public static PromptFormatResult Validate(string? question, GuardOptions options)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return PromptFormatResult.Invalid(PromptFormatFailure.Empty, "empty");
        }

        if (question.Length > options.MaxPromptChars)
        {
            return PromptFormatResult.Invalid(PromptFormatFailure.TooLong, "too-long");
        }

        if (CountLines(question) > options.MaxPromptLines)
        {
            return PromptFormatResult.Invalid(PromptFormatFailure.TooManyLines, "too-many-lines");
        }

        foreach (var c in question)
        {
            if (IsInvisible(c))
            {
                return PromptFormatResult.Invalid(PromptFormatFailure.ZeroWidth, "zero-width");
            }

            // Tab, CR and LF are ordinary in a multi-line message; everything else in the C0/C1
            // ranges is not something a chat client produces.
            if (!IsAllowedWhitespace(c) && char.IsControl(c))
            {
                return PromptFormatResult.Invalid(PromptFormatFailure.ControlCharacters, "control-characters");
            }
        }

        return PromptFormatResult.Valid;
    }

    /// <summary>
    /// Folds away the differences an attacker can hide behind: compatibility forms, invisible
    /// characters, whitespace runs, and case.
    /// </summary>
    /// <remarks>
    /// Shared deliberately. The security scanner, the untrusted-content neutraliser and the
    /// answer fact checker all have to agree on what two pieces of text being "the same" means -
    /// if they normalise differently, text that one layer clears is text another layer never sees.
    /// </remarks>
    public static string Normalise(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalised = text.Normalize(NormalizationForm.FormKC);

        var builder = new StringBuilder(normalised.Length);
        foreach (var c in normalised)
        {
            if (IsInvisible(c))
            {
                continue;
            }

            builder.Append(char.IsControl(c) && !IsAllowedWhitespace(c) ? ' ' : c);
        }

        return WhitespaceRunRegex()
            .Replace(builder.ToString(), " ")
            .Trim()
            .ToLower(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Cuts <paramref name="text"/> to <paramref name="maxChars"/>. Used at the storage boundary:
    /// assistant answers are model-generated and therefore unbounded.
    /// </summary>
    public static string Truncate(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || maxChars <= 0)
        {
            return string.Empty;
        }

        return text.Length <= maxChars ? text : text[..maxChars];
    }

    /// <summary>
    /// Zero-width, word-joiner, bidirectional-override/isolate marks and the byte-order mark.
    /// All of these render as nothing, and all of them survive into the token stream - which is
    /// exactly what makes them useful for hiding an instruction from whoever reads the message.
    /// </summary>
    /// <remarks>
    /// Compared as code points rather than character literals so this file stays pure ASCII.
    /// A source file that contains the characters it rejects is one careless copy-paste away
    /// from smuggling them somewhere else.
    /// </remarks>
    private static bool IsInvisible(char c)
        => (int)c is (>= 0x200B and <= 0x200F)   // zero-width space .. right-to-left mark
            or (>= 0x202A and <= 0x202E)          // bidi embedding and override
            or (>= 0x2060 and <= 0x2064)          // word joiner .. invisible plus
            or (>= 0x2066 and <= 0x2069)          // bidi isolates
            or 0xFEFF;                            // byte-order mark

    private static bool IsAllowedWhitespace(char c) => c is '\t' or '\r' or '\n';

    private static int CountLines(string text)
    {
        var lines = 1;
        foreach (var c in text)
        {
            if (c == '\n')
            {
                lines++;
            }
        }

        return lines;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRunRegex();
}
