using ChatAgent.App.Guards.Input;
using System.Text.RegularExpressions;

namespace ChatAgent.App.Guards.Untrusted;

/// <summary>
/// Strips the constructs that let untrusted text stop being data and start being structure.
/// </summary>
/// <remarks>
/// <para>
/// The trust rule this enforces: user input, knowledge base documents, replicated database text,
/// tool responses and web content are <b>untrusted</b>. Untrusted does not mean incorrect - it
/// means the text is never permitted to act as an instruction to the model.
/// </para>
/// <para>
/// A prompt asking a model to please treat something as data is not a control, because the
/// request and the attack arrive through the same channel and the model has no way to tell them
/// apart. What it <em>can</em> be told apart by is structure, so this class removes the structure
/// untrusted text would need in order to impersonate the prompt around it:
/// </para>
/// <list type="bullet">
/// <item>chat template tokens, which are how a message claims to be a different message;</item>
/// <item>line-leading markdown headings, which are how content forges a prompt section - a
/// product description containing <c>### GetAllProductsAsync</c> otherwise appears to the
/// response guard as a genuine tool result;</item>
/// <item>line-leading role labels, the plain-text equivalent of the same trick.</item>
/// </list>
/// <para>
/// Headings are escaped rather than deleted: a knowledge base document legitimately contains
/// <c>## Refund policy</c>, and the answer is worse if that text silently disappears than if it
/// reads as literal. The goal is to remove authority, not information.
/// </para>
/// </remarks>
public static partial class UntrustedText
{
    /// <summary>
    /// Renders <paramref name="content"/> inert. Safe to call on already-neutralised text.
    /// </summary>
    public static string Neutralize(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        // Invisible and control characters go first: everything below is a textual match, and a
        // zero-width space in the middle of "### " would defeat all of it.
        var text = StripInvisible(content);

        text = TemplateTokenRegex().Replace(text, string.Empty);
        text = HeadingRegex().Replace(text, "$1(heading) ");
        text = HorizontalRuleRegex().Replace(text, "$1(rule)");
        text = RoleLabelRegex().Replace(text, "$1(label) ");

        return text.Trim();
    }

    /// <summary>
    /// Removes invisible characters without the case-folding and whitespace collapsing that
    /// <see cref="PromptFormatValidator.Normalise"/> also applies.
    /// </summary>
    /// <remarks>
    /// Normalise exists for <em>comparison</em> - it deliberately destroys information so two
    /// spellings of the same thing match. This exists for <em>display</em>: the result is still
    /// shown to the model and quoted back to the customer, so casing, line breaks and formatting
    /// have to survive.
    /// </remarks>
    private static string StripInvisible(string text)
    {
        Span<char> buffer = text.Length <= 512 ? stackalloc char[text.Length] : new char[text.Length];
        var length = 0;

        foreach (var c in text)
        {
            if (IsInvisible(c))
            {
                continue;
            }

            buffer[length++] = char.IsControl(c) && c is not ('\t' or '\r' or '\n') ? ' ' : c;
        }

        return new string(buffer[..length]);
    }

    /// <summary>
    /// Mirrors <see cref="PromptFormatValidator"/>'s set. Compared as code points so this file
    /// contains none of the characters it removes.
    /// </summary>
    private static bool IsInvisible(char c)
        => (int)c is (>= 0x200B and <= 0x200F)
            or (>= 0x202A and <= 0x202E)
            or (>= 0x2060 and <= 0x2064)
            or (>= 0x2066 and <= 0x2069)
            or 0xFEFF;

    [GeneratedRegex(
        @"<\|(?:im_start|im_end|system|user|assistant|endoftext)\|>|\[/?INST\]|<</?SYS>>|</s>",
        RegexOptions.IgnoreCase)]
    private static partial Regex TemplateTokenRegex();

    [GeneratedRegex(@"(^|\n)[ \t]*#{1,6}[ \t]+", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"(^|\n)[ \t]*(?:-{3,}|={3,}|\*{3,}|_{3,})[ \t]*(?=\n|$)", RegexOptions.Multiline)]
    private static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"(^|\n)[ \t]*(?:system|assistant|tool|user)[ \t]*:[ \t]*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex RoleLabelRegex();
}
