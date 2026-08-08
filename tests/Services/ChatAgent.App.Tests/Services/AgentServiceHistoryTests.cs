using ChatAgent.App.Configurations;
using ChatAgent.App.Data.Enums;
using ChatAgent.App.Dtos;
using ChatAgent.App.Guards;
using ChatAgent.App.Guards.Grounding;
using ChatAgent.App.Guards.Interfaces;
using ChatAgent.App.Plugins;
using ChatAgent.App.Services;
using ChatAgent.App.Services.Interfaces;
using Mango.Core.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Shouldly;

namespace ChatAgent.App.Tests.Services;

/// <summary>
/// Pins how conversation history is handed to the model.
/// </summary>
/// <remarks>
/// Automatic function calling appends its bookkeeping — the assistant <c>tool_calls</c>
/// message and each tool result — to whatever <see cref="ChatHistory"/> it is given, and
/// <see cref="ChatHistoryMemoryStorage"/> caches one instance per user for the life of the
/// process. Passing the cached instance straight in let that bookkeeping accumulate, and a
/// failure part-way through invocation stranded a <c>tool_calls</c> message with no
/// matching tool result — a shape Azure OpenAI rejects, so every later turn for that user
/// failed until the process restarted.
/// </remarks>
public class AgentServiceHistoryTests
{
    /// <summary>
    /// Stands in for the real completion service and mutates the history it is handed,
    /// exactly as automatic function calling does.
    /// </summary>
    private sealed class HistoryMutatingChatService : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public ChatHistory? Received { get; private set; }

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            Received = chatHistory;

            // What auto function invocation does to the caller's history.
            chatHistory.AddAssistantMessage("[tool_calls bookkeeping]");
            chatHistory.AddMessage(AuthorRole.Tool, "[tool result]");

            IReadOnlyList<ChatMessageContent> result =
                [new ChatMessageContent(AuthorRole.Assistant, "We are open 10:00 to 22:00.")];

            return Task.FromResult(result);
        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("The agent buffers rather than streaming from the model.");
    }

    private static AgentService CreateAgentService(
        HistoryMutatingChatService chatService,
        IChatHistoryMemoryStorage historyStorage)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IChatCompletionService>(chatService);
        var kernel = new Kernel(services.BuildServiceProvider());

        var currentUser = new Mock<ICurrentUserContext>();
        currentUser.SetupGet(x => x.UserId).Returns("user-1");

        var searchService = new Mock<IKnowledgeSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<VectorSourceType[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var config = Options.Create(new AIAgentConfiguration());

        var relevanceGuard = new Mock<IRelevanceGuard>();
        relevanceGuard
            .Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GuardVerdict.Allow());

        var responseGuard = new Mock<IResponseGuard>();
        responseGuard
            .Setup(x => x.ReviewAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GroundingSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string draft, GroundingSnapshot _, CancellationToken _) => ReviewVerdict.Approve(draft));

        var groundingContext = new GroundingContext();

        return new AgentService(
            kernel,
            new CartPlugin(Mock.Of<ICartApi>(), currentUser.Object),
            historyStorage,
            new ProductsPlugin(TestChatAgentDbContext.Create(), searchService.Object),
            new CouponsPlugin(Mock.Of<ICouponsApi>()),
            new CheckoutPlugin(),
            new WebSearchPlugin(httpClientFactory.Object, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()),
            relevanceGuard.Object,
            responseGuard.Object,
            groundingContext,
            new GroundingCaptureFilter(groundingContext, config, NullLogger<GroundingCaptureFilter>.Instance),
            config,
            Mock.Of<IHostEnvironment>(e => e.EnvironmentName == "Production"),
            NullLogger<AgentService>.Instance);
    }

    private sealed class FakeHistoryStorage : IChatHistoryMemoryStorage
    {
        public ChatHistory History { get; } = new();

        public List<(ChatMessageRole Role, string Content)> Saved { get; } = [];

        public Task<ChatHistory> GetChatHistoryAsync(string userId) => Task.FromResult(History);

        public Task SaveMessageAsync(string userId, ChatMessageRole role, string content)
        {
            Saved.Add((role, content));
            return Task.CompletedTask;
        }

        public Task ClearHistoryAsync(string userId) => Task.CompletedTask;
    }

    private static async Task<string> DrainAsync(IAsyncEnumerable<string> chunks)
    {
        var text = string.Empty;
        await foreach (var chunk in chunks)
        {
            text += chunk;
        }

        return text;
    }

    [Fact]
    public async Task ChatStreamingAsync_When_ModelMutatesHistory_Then_CachedHistoryIsNotGivenToTheModel()
    {
        var chatService = new HistoryMutatingChatService();
        var storage = new FakeHistoryStorage();

        await DrainAsync(CreateAgentService(chatService, storage)
            .ChatStreamingAsync("user-1", new PromptRequestDto { Content = "what time do you open?" }));

        chatService.Received.ShouldNotBeNull();
        chatService.Received.ShouldNotBeSameAs(storage.History);
    }

    [Fact]
    public async Task ChatStreamingAsync_When_ModelMutatesHistory_Then_CachedHistoryKeepsOnlyUserAndAssistantText()
    {
        var chatService = new HistoryMutatingChatService();
        var storage = new FakeHistoryStorage();

        await DrainAsync(CreateAgentService(chatService, storage)
            .ChatStreamingAsync("user-1", new PromptRequestDto { Content = "what time do you open?" }));

        // Tool bookkeeping must never reach the cached history: it grows every prompt and,
        // if a call fails mid-invocation, leaves a shape the provider rejects.
        storage.History.ShouldNotContain(m => m.Role == AuthorRole.Tool);
        storage.History.Count.ShouldBe(2);
        storage.History[0].Role.ShouldBe(AuthorRole.User);
        storage.History[1].Role.ShouldBe(AuthorRole.Assistant);
        storage.History[1].Content.ShouldBe("We are open 10:00 to 22:00.");
    }

    [Fact]
    public async Task ChatStreamingAsync_When_TurnCompletes_Then_PersistsUserAndFinalAnswerOnly()
    {
        var chatService = new HistoryMutatingChatService();
        var storage = new FakeHistoryStorage();

        var answer = await DrainAsync(CreateAgentService(chatService, storage)
            .ChatStreamingAsync("user-1", new PromptRequestDto { Content = "what time do you open?" }));

        answer.ShouldBe("We are open 10:00 to 22:00.");
        storage.Saved.Count.ShouldBe(2);
        storage.Saved[0].Role.ShouldBe(ChatMessageRole.User);
        storage.Saved[1].Role.ShouldBe(ChatMessageRole.Assistant);
        storage.Saved[1].Content.ShouldBe("We are open 10:00 to 22:00.");
    }
}
