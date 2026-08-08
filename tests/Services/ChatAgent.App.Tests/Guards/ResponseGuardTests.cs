using ChatAgent.App.Configurations;
using ChatAgent.App.Guards;
using ChatAgent.App.Guards.Grounding;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Moq;
using Shouldly;

namespace ChatAgent.App.Tests.Guards;

public class ResponseGuardTests
{
    private const string Draft = "Our Pho Bo is 12.50 and it's delicious! 🍜";

    private static Mock<GuardChatClient> CreateChatClient(string? response)
    {
        var mock = new Mock<GuardChatClient>(
            Kernel.CreateBuilder().Build(),
            Options.Create(new AIAgentConfiguration()),
            NullLogger<GuardChatClient>.Instance);

        mock.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        return mock;
    }

    private static ResponseGuard CreateGuard(Mock<GuardChatClient> chatClient, GuardOptions? options = null)
        => new(
            chatClient.Object,
            Options.Create(new AIAgentConfiguration { Guard = options ?? new GuardOptions() }),
            NullLogger<ResponseGuard>.Instance);

    private static GroundingSnapshot Grounding()
        => new([new GroundingEntry("SearchProductsAsync", """[{"Name":"Pho Bo","Price":12.50}]""")], false);

    [Fact]
    public async Task ReviewAsync_When_GuardApproves_Then_ReturnsTheOriginalDraft()
    {
        var chatClient = CreateChatClient("""{"verdict":"approved","content":"whatever","reason":"grounded"}""");

        var result = await CreateGuard(chatClient).ReviewAsync("how much is pho?", Draft, Grounding());

        result.Kind.ShouldBe(ReviewVerdictKind.Approved);
        result.Content.ShouldBe(Draft);
    }

    [Fact]
    public async Task ReviewAsync_When_GuardRevises_Then_ReturnsTheRewrittenText()
    {
        var chatClient = CreateChatClient(
            """{"verdict":"revised","content":"Our Pho Bo is 12.50.","reason":"dropped stock claim"}""");

        var result = await CreateGuard(chatClient).ReviewAsync("how much is pho?", Draft, Grounding());

        result.Kind.ShouldBe(ReviewVerdictKind.Revised);
        result.Content.ShouldBe("Our Pho Bo is 12.50.");
    }

    [Fact]
    public async Task ReviewAsync_When_GuardRejects_Then_ReturnsTheConfiguredFallback()
    {
        var options = new GuardOptions { UnverifiedMessage = "Sorry, I can't confirm that." };
        var chatClient = CreateChatClient("""{"verdict":"rejected","content":"","reason":"invented price"}""");

        var result = await CreateGuard(chatClient, options).ReviewAsync("how much is caviar?", Draft, Grounding());

        result.Kind.ShouldBe(ReviewVerdictKind.Rejected);
        result.Content.ShouldBe("Sorry, I can't confirm that.");

        // The unverified draft must not survive in any form.
        result.Content.ShouldNotContain("12.50");
    }

    [Fact]
    public async Task ReviewAsync_When_GuardRevisesWithNoContent_Then_TreatsItAsUnusable()
    {
        var options = new GuardOptions { FailOpen = false, UnverifiedMessage = "fallback" };
        var chatClient = CreateChatClient("""{"verdict":"revised","content":"","reason":"oops"}""");

        var result = await CreateGuard(chatClient, options).ReviewAsync("q", Draft, Grounding());

        result.Kind.ShouldBe(ReviewVerdictKind.Rejected);
        result.Content.ShouldBe("fallback");
    }

    [Fact]
    public async Task ReviewAsync_When_GuardFailsAndFailOpenIsTrue_Then_SendsTheDraft()
    {
        var chatClient = CreateChatClient(null);

        var result = await CreateGuard(chatClient, new GuardOptions { FailOpen = true })
            .ReviewAsync("q", Draft, Grounding());

        result.Kind.ShouldBe(ReviewVerdictKind.Approved);
        result.Content.ShouldBe(Draft);
    }

    [Fact]
    public async Task ReviewAsync_When_GuardFailsAndFailOpenIsFalse_Then_SendsTheFallback()
    {
        var options = new GuardOptions { FailOpen = false, UnverifiedMessage = "fallback" };
        var chatClient = CreateChatClient(null);

        var result = await CreateGuard(chatClient, options).ReviewAsync("q", Draft, Grounding());

        result.Kind.ShouldBe(ReviewVerdictKind.Rejected);
        result.Content.ShouldBe("fallback");
    }

    [Fact]
    public async Task ReviewAsync_When_GuardIsDisabled_Then_ApprovesWithoutCallingTheModel()
    {
        var chatClient = CreateChatClient(null);

        var result = await CreateGuard(chatClient, new GuardOptions { OutputEnabled = false })
            .ReviewAsync("q", Draft, Grounding());

        result.Content.ShouldBe(Draft);
        chatClient.Verify(
            x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

public class GroundingContextTests
{
    [Fact]
    public void Record_When_ResultIsEmpty_Then_IsIgnored()
    {
        var context = new GroundingContext();

        context.Record("Tool", null);
        context.Record("Tool", "   ");

        context.Snapshot().HasFacts.ShouldBeFalse();
    }

    [Fact]
    public void Record_When_ResultIsLarge_Then_TruncatesAndFlagsIt()
    {
        var context = new GroundingContext();

        context.Record("Tool", new string('x', 10_000));

        var snapshot = context.Snapshot();
        snapshot.Truncated.ShouldBeTrue();
        snapshot.Entries[0].Result.Length.ShouldBeLessThan(10_000);
    }

    [Fact]
    public void ToPromptText_When_NoToolsRan_Then_SaysSoExplicitly()
    {
        // The guard has to be able to tell "no facts" apart from "facts that say nothing".
        GroundingSnapshot.Empty.ToPromptText().ShouldContain("no tools were called");
    }

    [Fact]
    public void ToPromptText_When_ToolsRan_Then_IncludesToolNamesAndResults()
    {
        var context = new GroundingContext();
        context.Record("SearchProductsAsync", """[{"Name":"Pho Bo"}]""");

        var text = context.Snapshot().ToPromptText();

        text.ShouldContain("SearchProductsAsync");
        text.ShouldContain("Pho Bo");
    }
}
