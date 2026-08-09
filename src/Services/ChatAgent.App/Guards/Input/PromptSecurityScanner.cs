using System.Text.RegularExpressions;

namespace ChatAgent.App.Guards.Input;

/// <summary>
/// Deterministic prompt-injection and exfiltration detection.
/// </summary>
/// <remarks>
/// <para>
/// This is the only injection check that cannot be talked out of its verdict. The LLM classifier
/// in <see cref="RelevanceGuard"/> is more capable and far more flexible, but it is itself a model
/// being shown attacker-controlled text; this layer is a regex, and a regex cannot be persuaded.
/// It therefore runs first and is not subject to <see cref="GuardOptions.FailOpen"/>.
/// </para>
/// <para>
/// The same scanner runs over <em>untrusted content</em> at its ingest boundaries - knowledge base
/// documents, replicated product text, web results - so there is exactly one rule set to maintain
/// and to reason about. See <c>Guards/Untrusted</c>.
/// </para>
/// <para>
/// Two deliberate limits, both documented rather than papered over:
/// </para>
/// <list type="bullet">
/// <item>Encoded payloads are flagged by <em>shape</em>, never decoded. Decoding is where
/// determinism stops being deterministic - a decoder has to guess an encoding, and a wrong guess
/// is a rule that fires on ordinary text.</item>
/// <item>An injection split across several turns is invisible here, because this layer sees one
/// message at a time. The LLM classifier does receive recent turns and covers that case.</item>
/// </list>
/// </remarks>
public static partial class PromptSecurityScanner
{
    /// <summary>
    /// Runs every rule over the normalised text. First match wins; rules are ordered most to
    /// least specific so the reported rule id is the most informative one.
    /// </summary>
    public static PromptScanResult Scan(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return PromptScanResult.Clean;
        }

        var normalised = PromptFormatValidator.Normalise(text);

        if (TemplateTokenRegex().IsMatch(normalised))
        {
            return PromptScanResult.Block(
                GuardCategory.PromptInjection, "template-tokens", "chat template tokens in content");
        }

        if (OverrideInstructionsRegex().IsMatch(normalised))
        {
            return PromptScanResult.Block(
                GuardCategory.PromptInjection, "override-instructions", "attempt to override instructions");
        }

        if (PromptExfiltrationRegex().IsMatch(normalised))
        {
            return PromptScanResult.Block(
                GuardCategory.PromptInjection, "prompt-exfiltration", "attempt to extract the system prompt");
        }

        if (RoleHijackRegex().IsMatch(normalised))
        {
            return PromptScanResult.Block(
                GuardCategory.PromptInjection, "role-hijack", "attempt to reassign the agent's role");
        }

        if (ToolDirectiveRegex().IsMatch(normalised))
        {
            return PromptScanResult.Block(
                GuardCategory.PromptInjection, "tool-directive", "attempt to command tool invocation");
        }

        if (PriceTamperingRegex().IsMatch(normalised))
        {
            return PromptScanResult.Block(
                GuardCategory.PromptInjection, "price-tampering", "attempt to alter pricing");
        }

        if (EncodingEvasionRegex().IsMatch(normalised))
        {
            return PromptScanResult.Block(
                GuardCategory.PromptInjection, "encoding-evasion", "encoded payload");
        }

        if (DataExfiltrationRegex().IsMatch(normalised))
        {
            return PromptScanResult.Block(
                GuardCategory.Unsafe, "data-exfiltration", "attempt to extract secrets or other customers' data");
        }

        return PromptScanResult.Clean;
    }

    /// <summary>True when the text trips any rule. For ingest-boundary callers that only need a verdict.</summary>
    public static bool IsSuspicious(string? text) => Scan(text).Blocked;

    // Literal control tokens for the common chat templates. These have no legitimate use in a
    // customer message, a product description, or a store document.
    [GeneratedRegex(
        @"<\|(?:im_start|im_end|system|user|assistant|endoftext)\|>|\[/?INST\]|<</?SYS>>|</s>|^(?:system|assistant|tool)\s*:",
        RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex TemplateTokenRegex();

    [GeneratedRegex(
        @"\b(?:ignore|disregard|forget|override|bypass)\b[^.]{0,40}\b(?:previous|prior|above|earlier|all|your)\b[^.]{0,40}\b(?:instruction|prompt|rule|direction|context|message)s?\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex OverrideInstructionsRegex();

    [GeneratedRegex(
        @"\b(?:system|initial|original)\s+(?:prompt|message|instruction)|\brepeat everything above\b|\bprint\b[^.]{0,20}\b(?:your|the)\b[^.]{0,20}\b(?:prompt|instructions|configuration)\b|\bwhat (?:are|were) your (?:instructions|rules)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex PromptExfiltrationRegex();

    [GeneratedRegex(
        @"\byou are now\b|\bdeveloper mode\b|\bjailbreak\b|\bact as\b[^.]{0,30}\b(?:admin|root|developer|system|dba|staff|employee)\b|\bpretend (?:you|to be)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex RoleHijackRegex();

    [GeneratedRegex(
        @"\b(?:call|invoke|execute|run|trigger)\b[^.]{0,20}\b(?:tool|function|plugin|kernel)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex ToolDirectiveRegex();

    [GeneratedRegex(
        @"\b(?:set|change|make|update|apply)\b[^.]{0,25}\b(?:price|discount|total)\b|\b100\s*%\s*(?:off|discount)\b|\bfor free\b[^.]{0,25}\b(?:order|checkout|add|cart)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex PriceTamperingRegex();

    // Shape only. A long unbroken base64-ish run or a dense percent-encoded run is not something
    // a customer types; what it decodes to is deliberately not this layer's business.
    [GeneratedRegex(@"[A-Za-z0-9+/]{40,}={0,2}|(?:%[0-9a-f]{2}){12,}", RegexOptions.IgnoreCase)]
    private static partial Regex EncodingEvasionRegex();

    [GeneratedRegex(
        @"\b(?:connection string|api key|secret key|access token|password|env(?:ironment)? variable)\b|\b(?:drop|truncate)\s+table\b|\bunion\s+select\b|\bselect\b[^.]{0,30}\bfrom\b[^.]{0,20}\b(?:users?|customers?|orders?)\b|\b(?:someone else'?s|another (?:user|customer)'?s)\b[^.]{0,25}\b(?:order|cart|account|address|phone|email)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex DataExfiltrationRegex();
}
