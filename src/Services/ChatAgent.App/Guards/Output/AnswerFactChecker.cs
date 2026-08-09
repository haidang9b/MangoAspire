using ChatAgent.App.Guards.Authorization;
using ChatAgent.App.Guards.Grounding;
using ChatAgent.App.Guards.Input;
using Microsoft.Extensions.Options;

namespace ChatAgent.App.Guards.Output;

/// <inheritdoc cref="IAnswerFactChecker"/>
/// <remarks>
/// <para>
/// The response guard was, until this existed, a model being asked whether another model had made
/// something up - while being shown the same retrieved facts, which may themselves carry an
/// injection. This layer is the part of that verification that cannot be argued with: a price the
/// draft quotes either appears in the captured tool output or it does not.
/// </para>
/// <para>
/// Findings come in two strengths. <b>Hard</b> findings end the review immediately with no model
/// call, because there is nothing to salvage - a leaked GUID is not a phrasing problem. <b>Soft</b>
/// findings are passed to the reviewer as evidence, and the reviewer may not approve a draft that
/// still contains one.
/// </para>
/// </remarks>
public class AnswerFactChecker : IAnswerFactChecker
{
    private readonly GuardOptions _options;

    public AnswerFactChecker(IOptions<AIAgentConfiguration> options) => _options = options.Value.Guard;

    public FactCheckResult Check(string? answer, GroundingSnapshot grounding)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return FactCheckResult.Pass;
        }

        var issues = new List<FactCheckIssue>();
        var haystack = ClaimExtractor.Flatten(grounding);

        CheckIdentifierLeaks(answer, issues);
        CheckInternalLeaks(answer, issues);
        CheckStockClaims(answer, haystack, issues);
        CheckGroundedValues(answer, haystack, issues);
        CheckUngroundedWhenNoTools(answer, grounding, issues);

        return issues.Count == 0 ? FactCheckResult.Pass : new FactCheckResult(issues);
    }

    /// <summary>
    /// Internal identifiers have no customer-facing use, so their presence is conclusive without
    /// reference to the grounding - the tool output legitimately contains them.
    /// </summary>
    private static void CheckIdentifierLeaks(string answer, List<FactCheckIssue> issues)
    {
        foreach (var guid in ClaimExtractor.ExtractGuids(answer))
        {
            issues.Add(new FactCheckIssue(
                "id-leak", guid, "an internal identifier that must never be shown to a customer", IsHard: true));
        }
    }

    private static void CheckInternalLeaks(string answer, List<FactCheckIssue> issues)
    {
        foreach (var functionName in ToolCatalog.AllFunctionNames)
        {
            if (answer.Contains(functionName, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new FactCheckIssue(
                    "internal-leak", functionName, "the name of an internal tool", IsHard: true));
            }
        }

        foreach (var marker in InternalMarkers)
        {
            if (answer.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new FactCheckIssue(
                    "internal-leak", marker, "a reference to the assistant's own instructions", IsHard: true));
            }
        }
    }

    private void CheckStockClaims(string answer, string haystack, List<FactCheckIssue> issues)
    {
        var claims = ClaimExtractor.ExtractStockClaims(answer);
        if (claims.Count == 0)
        {
            return;
        }

        // Checked once for the whole answer rather than per claim: what makes any availability
        // statement grounded is that a tool returned a stock number at all. A product whose stock
        // has never been replicated serialises as null, which does not match - so "we have some
        // left" about an unknown is caught, which is the case that matters.
        var grounded = _options.StockClaimsAllowed && ClaimExtractor.GroundingHasStockValue(haystack);
        if (grounded)
        {
            return;
        }

        foreach (var claim in claims)
        {
            issues.Add(new FactCheckIssue(
                "stock-claim",
                claim,
                _options.StockClaimsAllowed
                    ? "an availability claim, but no tool returned a stock value for that item"
                    : "an availability claim, and availability answers are currently switched off",
                IsHard: false));
        }
    }

    private static void CheckGroundedValues(string answer, string haystack, List<FactCheckIssue> issues)
    {
        foreach (var (text, value) in ClaimExtractor.ExtractMoney(answer))
        {
            if (!ClaimExtractor.GroundingContainsMoney(haystack, value))
            {
                issues.Add(new FactCheckIssue(
                    "ungrounded-price", text, "a price that appears nowhere in the retrieved facts", IsHard: false));
            }
        }

        foreach (var percentage in ClaimExtractor.ExtractPercentages(answer))
        {
            if (!ClaimExtractor.GroundingContains(haystack, percentage))
            {
                issues.Add(new FactCheckIssue(
                    "ungrounded-percentage", percentage, "a discount or proportion that is not in the retrieved facts", IsHard: false));
            }
        }

        foreach (var time in ClaimExtractor.ExtractTimes(answer))
        {
            if (!ClaimExtractor.GroundingContains(haystack, time))
            {
                issues.Add(new FactCheckIssue(
                    "ungrounded-time", time, "an opening time that is not in the retrieved facts", IsHard: false));
            }
        }

        foreach (var phone in ClaimExtractor.ExtractPhoneNumbers(answer))
        {
            if (!ClaimExtractor.GroundingContains(haystack, phone))
            {
                issues.Add(new FactCheckIssue(
                    "ungrounded-contact", phone, "a contact number that is not in the retrieved facts", IsHard: false));
            }
        }
    }

    /// <summary>
    /// A draft that makes concrete claims when no tool ran has nothing behind it at all.
    /// </summary>
    /// <remarks>
    /// Greetings, clarifying questions and refusals produce no claims, so they pass here and cost
    /// nothing - which is what keeps this rule from penalising the turns that legitimately need
    /// no grounding.
    /// </remarks>
    private static void CheckUngroundedWhenNoTools(
        string answer,
        GroundingSnapshot grounding,
        List<FactCheckIssue> issues)
    {
        if (grounding.HasFacts || !ClaimExtractor.HasAnyClaim(answer))
        {
            return;
        }

        issues.Add(new FactCheckIssue(
            "ungrounded",
            PromptFormatValidator.Truncate(answer, 80),
            "a factual claim, but no tool was called during this turn",
            IsHard: false));
    }

    private static readonly string[] InternalMarkers =
    [
        "system prompt",
        "system message",
        "my instructions",
        "grounding rules",
    ];
}
