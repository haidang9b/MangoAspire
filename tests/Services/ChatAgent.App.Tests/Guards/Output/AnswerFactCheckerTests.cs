using ChatAgent.App.Configurations;
using ChatAgent.App.Guards.Grounding;
using ChatAgent.App.Guards.Output;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ChatAgent.App.Tests.Guards.Output;

public class AnswerFactCheckerTests
{
    private static AnswerFactChecker CreateChecker(GuardOptions? options = null)
        => new(Options.Create(new AIAgentConfiguration { Guard = options ?? new GuardOptions() }));

    private static GroundingSnapshot Grounding(string result, string tool = "SearchProductsAsync")
    {
        var context = new GroundingContext();
        context.Record(tool, result);
        return context.Snapshot();
    }

    [Fact]
    public void Check_When_AnswerQuotesAPriceFromGrounding_Then_Passes()
    {
        var checker = CreateChecker();
        var grounding = Grounding("""[{"Name":"Pho Bo","Price":12.50}]""");

        checker.Check("Our Pho Bo is $12.50.", grounding).Passed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Our Pho Bo is $12.50.")]
    [InlineData("Our Pho Bo is 12,50 euros.")]
    [InlineData("Our Pho Bo is 12.5 dollars.")]
    public void Check_When_PriceFormattingDiffers_Then_StillPasses(string answer)
    {
        // The tool returns a bare JSON number; the assistant writes money. Comparing textually
        // would reject every correct answer, so these are compared as decimals.
        var grounding = Grounding("""[{"Name":"Pho Bo","Price":12.50}]""");

        CreateChecker().Check(answer, grounding).Passed.ShouldBeTrue();
    }

    [Fact]
    public void Check_When_AnswerQuotesAPriceNotInGrounding_Then_FailsAsUngroundedPrice()
    {
        var grounding = Grounding("""[{"Name":"Pho Bo","Price":12.50}]""");

        var result = CreateChecker().Check("Our Pho Bo is $9.99 today.", grounding);

        result.Passed.ShouldBeFalse();
        result.RuleIds.ShouldContain("ungrounded-price");
        result.HasHardViolation.ShouldBeFalse();
    }

    [Fact]
    public void Check_When_AnswerContainsAGuid_Then_FailsAsAHardIdLeak()
    {
        var id = Guid.NewGuid();
        var grounding = Grounding($$"""[{"Id":"{{id}}","Name":"Pho Bo","Price":12.50}]""");

        // Grounded, and still forbidden: an internal identifier has no customer-facing use, so
        // there is nothing for a reviewer to salvage and no reason to spend a model call.
        var result = CreateChecker().Check($"Your item id is {id}.", grounding);

        result.HasHardViolation.ShouldBeTrue();
        result.RuleIds.ShouldContain("id-leak");
    }

    [Fact]
    public void Check_When_AnswerNamesAKernelFunction_Then_FailsAsAHardInternalLeak()
    {
        var result = CreateChecker().Check(
            "I called SearchProductsAsync to find that.", Grounding("""[{"Name":"Pho Bo"}]"""));

        result.HasHardViolation.ShouldBeTrue();
        result.RuleIds.ShouldContain("internal-leak");
    }

    [Fact]
    public void Check_When_AnswerClaimsStockWithNoStockValueInGrounding_Then_FailsAsStockClaim()
    {
        var grounding = Grounding("""[{"Name":"Pho Bo","Price":12.50,"AvailableStock":null}]""");

        var result = CreateChecker().Check("Pho Bo is in stock right now.", grounding);

        result.Passed.ShouldBeFalse();
        result.RuleIds.ShouldContain("stock-claim");
    }

    [Fact]
    public void Check_When_AnswerClaimsStockAndGroundingCarriesAStockValue_Then_Passes()
    {
        var grounding = Grounding("""[{"Name":"Pho Bo","Price":12.50,"AvailableStock":7}]""");

        CreateChecker().Check("Pho Bo is in stock.", grounding).Passed.ShouldBeTrue();
    }

    [Fact]
    public void Check_When_StockClaimsAreDisabled_Then_FailsEvenWithAStockValue()
    {
        var grounding = Grounding("""[{"Name":"Pho Bo","AvailableStock":7}]""");
        var checker = CreateChecker(new GuardOptions { StockClaimsAllowed = false });

        checker.Check("Pho Bo is in stock.", grounding).RuleIds.ShouldContain("stock-claim");
    }

    [Fact]
    public void Check_When_NoToolsRanAndAnswerIsAGreeting_Then_Passes()
    {
        // Greetings, clarifying questions and refusals make no claims, so they need no grounding
        // and must not be penalised for having none.
        CreateChecker()
            .Check("Hi! What can I get you today?", GroundingSnapshot.Empty)
            .Passed.ShouldBeTrue();
    }

    [Fact]
    public void Check_When_NoToolsRanAndAnswerStatesAPrice_Then_FailsAsUngrounded()
    {
        var result = CreateChecker().Check("The Pho Bo is $12.50.", GroundingSnapshot.Empty);

        result.Passed.ShouldBeFalse();
        result.RuleIds.ShouldContain("ungrounded");
    }

    [Fact]
    public void Check_When_AnswerStatesAnOpeningTimeNotInGrounding_Then_FailsAsUngroundedTime()
    {
        var grounding = Grounding("We open at 10:00 and close at 22:00.", "SearchStoreInfoAsync");

        CreateChecker()
            .Check("We're open from 8:00 tomorrow.", grounding)
            .RuleIds.ShouldContain("ungrounded-time");
    }

    [Fact]
    public void Check_When_AnswerIsBlank_Then_Passes()
        => CreateChecker().Check("  ", GroundingSnapshot.Empty).Passed.ShouldBeTrue();
}
