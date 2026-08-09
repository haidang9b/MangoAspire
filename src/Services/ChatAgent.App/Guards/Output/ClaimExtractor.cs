using ChatAgent.App.Guards.Grounding;
using ChatAgent.App.Guards.Input;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatAgent.App.Guards.Output;

/// <summary>
/// Pulls checkable claims out of a drafted answer, and flattens the captured tool results into
/// something they can be checked against.
/// </summary>
/// <remarks>
/// Every extractor is conservative in the same direction: it would rather miss a claim than
/// invent one. A missed claim costs a verification the LLM reviewer still performs; a fabricated
/// one blocks a correct answer, and a guard that blocks correct answers gets switched off.
/// </remarks>
internal static partial class ClaimExtractor
{
    /// <summary>
    /// Money written next to a currency marker. Bare numbers are deliberately not money - "serves
    /// 4" and "12 minutes" are not prices, and treating them as such fires on every draft.
    /// </summary>
    public static IReadOnlyList<(string Text, decimal Value)> ExtractMoney(string text)
    {
        var results = new List<(string, decimal)>();

        foreach (Match match in MoneyRegex().Matches(text))
        {
            var raw = match.Groups["amount"].Value;
            if (TryParseMoney(raw, out var value))
            {
                results.Add((match.Value.Trim(), value));
            }
        }

        return results;
    }

    public static IReadOnlyList<string> ExtractPercentages(string text)
        => [.. PercentageRegex().Matches(text).Select(m => m.Value.Trim())];

    public static IReadOnlyList<string> ExtractTimes(string text)
        => [.. TimeRegex().Matches(text).Select(m => m.Value.Trim())];

    public static IReadOnlyList<string> ExtractPhoneNumbers(string text)
        => [.. PhoneRegex().Matches(text).Select(m => m.Value.Trim())];

    public static IReadOnlyList<string> ExtractGuids(string text)
        => [.. GuidRegex().Matches(text).Select(m => m.Value)];

    public static IReadOnlyList<string> ExtractStockClaims(string text)
        => [.. StockClaimRegex().Matches(text).Select(m => m.Value.Trim())];

    /// <summary>True when the draft asserts anything checkable at all.</summary>
    /// <remarks>
    /// Drives the "no tools ran" rule. A greeting, a clarifying question or a refusal makes no
    /// claims and needs no grounding, so it must not be penalised for having none.
    /// </remarks>
    public static bool HasAnyClaim(string text)
        => ExtractMoney(text).Count > 0
            || ExtractPercentages(text).Count > 0
            || ExtractTimes(text).Count > 0
            || ExtractPhoneNumbers(text).Count > 0
            || ExtractGuids(text).Count > 0
            || ExtractStockClaims(text).Count > 0;

    /// <summary>
    /// Concatenates every captured tool result into one normalised haystack for containment tests.
    /// </summary>
    public static string Flatten(GroundingSnapshot grounding)
    {
        if (grounding.Entries.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var entry in grounding.Entries)
        {
            builder.Append(entry.Result).Append('\n');
        }

        return PromptFormatValidator.Normalise(builder.ToString());
    }

    /// <summary>
    /// Whether a money value appears in the grounding, comparing numerically rather than
    /// textually so <c>$12.50</c>, <c>12,50</c> and <c>12.5</c> are the same amount.
    /// </summary>
    public static bool GroundingContainsMoney(string flattenedGrounding, decimal value)
    {
        foreach (var (_, groundedValue) in ExtractAllNumbers(flattenedGrounding))
        {
            if (groundedValue == value)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether the grounding carries an actual stock value for something.
    /// </summary>
    /// <remarks>
    /// Availability cannot be checked by text containment the way a price can: the assistant
    /// writes "in stock" while the tool returned <c>"availableStock":100</c>, so a containment
    /// test would reject every correct answer. What makes the claim grounded is that a tool
    /// returned a stock number at all - a null or absent value means nothing supports it.
    /// </remarks>
    public static bool GroundingHasStockValue(string flattenedGrounding)
        => StockValueRegex().IsMatch(flattenedGrounding);

    /// <summary>
    /// Whether a claim's significant text appears in the grounding. Normalised on both sides so
    /// casing and spacing differences do not count as a contradiction.
    /// </summary>
    public static bool GroundingContains(string flattenedGrounding, string claim)
    {
        var needle = PromptFormatValidator.Normalise(claim);

        if (needle.Length == 0)
        {
            return true;
        }

        // Punctuation varies freely between a tool's JSON and the assistant's prose ("9:00 am"
        // vs "9:00am"), so compare on the alphanumerics alone.
        return Compact(flattenedGrounding).Contains(Compact(needle), StringComparison.Ordinal);
    }

    /// <summary>
    /// Every number in the grounding, currency-marked or not. The grounding side is deliberately
    /// broader than the draft side: a price arrives as a bare <c>"price":12.5</c> in serialised
    /// tool output, and the draft's "$12.50" has to be able to match it.
    /// </summary>
    private static IReadOnlyList<(string Text, decimal Value)> ExtractAllNumbers(string text)
    {
        var results = new List<(string, decimal)>();

        foreach (Match match in BareNumberRegex().Matches(text))
        {
            if (TryParseMoney(match.Value, out var value))
            {
                results.Add((match.Value, value));
            }
        }

        return results;
    }

    private static bool TryParseMoney(string raw, out decimal value)
    {
        var cleaned = raw.Replace(" ", string.Empty);

        // A comma is a decimal separator in some locales and a thousands separator in others.
        // Treat "12,50" as 12.50 and "1,250" as 1250 by looking at the digits that follow it.
        if (cleaned.Contains(',') && !cleaned.Contains('.'))
        {
            var index = cleaned.LastIndexOf(',');
            var fractionDigits = cleaned.Length - index - 1;
            cleaned = fractionDigits is 1 or 2
                ? cleaned.Remove(index, 1).Insert(index, ".")
                : cleaned.Replace(",", string.Empty);
        }
        else
        {
            cleaned = cleaned.Replace(",", string.Empty);
        }

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static string Compact(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    [GeneratedRegex(
        @"(?:[$£€]\s?(?<amount>\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{1,2})?|\d+(?:[.,]\d{1,2})?))|(?:(?<amount>\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{1,2})?|\d+(?:[.,]\d{1,2})?)\s?(?:dollars?|usd|euros?|eur|pounds?|gbp))",
        RegexOptions.IgnoreCase)]
    private static partial Regex MoneyRegex();

    [GeneratedRegex(@"\d{1,3}(?:[.,]\d+)?\s?%")]
    private static partial Regex PercentageRegex();

    [GeneratedRegex(@"\b\d{1,2}(?::\d{2})?\s?(?:am|pm)\b|\b\d{1,2}:\d{2}\b", RegexOptions.IgnoreCase)]
    private static partial Regex TimeRegex();

    [GeneratedRegex(@"\+?\d[\d\s().-]{7,}\d")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b", RegexOptions.IgnoreCase)]
    private static partial Regex GuidRegex();

    [GeneratedRegex(
        @"\b(?:in stock|out of stock|sold out|sold-out|we have \d+|only \d+ left|\d+ (?:left|remaining|available)|currently (?:un)?available)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex StockClaimRegex();

    [GeneratedRegex(@"\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{1,2})?|\d+(?:[.,]\d{1,2})?")]
    private static partial Regex BareNumberRegex();

    // Matches the serialised DTO field with a number after it, and deliberately not with null.
    [GeneratedRegex(@"availablestock""?\s*[:=]\s*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex StockValueRegex();
}
