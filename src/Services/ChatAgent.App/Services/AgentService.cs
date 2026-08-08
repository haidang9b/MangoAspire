using ChatAgent.App.Data.Enums;
using ChatAgent.App.Dtos;
using ChatAgent.App.Guards;
using ChatAgent.App.Guards.Grounding;
using ChatAgent.App.Guards.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChatAgent.App.Services;

/// <summary>
/// Drives one customer turn: relevance check, agent run, answer verification, then
/// delivery.
/// </summary>
/// <remarks>
/// The answer is generated in full and verified before the first chunk leaves the service.
/// That costs a second or two of time-to-first-token and buys the guarantee that nothing
/// unverified is ever shown — there is no retraction path once text has been streamed to a
/// browser. The wire format is unchanged, so the SPA needs no modification.
/// </remarks>
public class AgentService : IAgentService
{
    /// <summary>
    /// Delivery chunk size. Small enough that the reply still renders progressively rather
    /// than appearing in one jump.
    /// </summary>
    private const int StreamChunkSize = 48;

    private readonly Kernel _kernel;
    private readonly ICartPlugin _cartPlugin;
    private readonly IProductsPlugin _productsPlugin;
    private readonly ICouponsPlugin _couponsPlugin;
    private readonly ICheckoutPlugin _checkoutPlugin;
    private readonly IWebSearchPlugin _webSearchPlugin;
    private readonly IChatHistoryMemoryStorage _chatHistory;
    private readonly IRelevanceGuard _relevanceGuard;
    private readonly IResponseGuard _responseGuard;
    private readonly IGroundingContext _groundingContext;
    private readonly GroundingCaptureFilter _groundingFilter;
    private readonly GuardOptions _guardOptions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        Kernel kernel,
        ICartPlugin cartPlugin,
        IChatHistoryMemoryStorage chatHistory,
        IProductsPlugin productPlugin,
        ICouponsPlugin couponsPlugin,
        ICheckoutPlugin checkoutPlugin,
        IWebSearchPlugin webSearchPlugin,
        IRelevanceGuard relevanceGuard,
        IResponseGuard responseGuard,
        IGroundingContext groundingContext,
        GroundingCaptureFilter groundingFilter,
        IOptions<AIAgentConfiguration> options,
        IHostEnvironment environment,
        ILogger<AgentService> logger)
    {
        _environment = environment;
        _kernel = kernel;
        _cartPlugin = cartPlugin;
        _chatHistory = chatHistory;
        _productsPlugin = productPlugin;
        _couponsPlugin = couponsPlugin;
        _checkoutPlugin = checkoutPlugin;
        _webSearchPlugin = webSearchPlugin;
        _relevanceGuard = relevanceGuard;
        _responseGuard = responseGuard;
        _groundingContext = groundingContext;
        _groundingFilter = groundingFilter;
        _guardOptions = options.Value.Guard;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> ChatStreamingAsync(
        string userId,
        PromptRequestDto promptRequest,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Everything that can fail happens here, outside the iterator, so error handling
        // is ordinary try/catch rather than something wrapped around a yield.
        var answer = await BuildVerifiedAnswerAsync(userId, promptRequest, cancellationToken);

        foreach (var chunk in SplitForDelivery(answer))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return chunk;
        }
    }

    private async Task<string> BuildVerifiedAnswerAsync(
        string userId,
        PromptRequestDto promptRequest,
        CancellationToken cancellationToken)
    {
        var question = promptRequest.Content ?? string.Empty;
        var chatHistory = await _chatHistory.GetChatHistoryAsync(userId);

        // Persisted before the guard runs so the transcript reflects what the customer
        // actually asked, including questions that were turned away.
        await _chatHistory.SaveMessageAsync(userId, ChatMessageRole.User, question);

        var verdict = await _relevanceGuard.EvaluateAsync(
            question,
            [.. chatHistory.TakeLast(_guardOptions.HistoryLookback).Select(m => $"{m.Role}: {m.Content}")],
            cancellationToken);

        if (!verdict.Allowed)
        {
            _logger.LogInformation(
                "Relevance guard blocked a question as {Category}: {Reason}", verdict.Category, verdict.Reason);

            var refusal = verdict.Category == GuardCategory.OffTopic
                ? _guardOptions.OffTopicMessage
                : _guardOptions.BlockedMessage;

            // The agent is never invoked, so no tools run and no tokens are spent on it.
            // Both turns still go into history so the conversation stays coherent.
            chatHistory.AddUserMessage(question);
            chatHistory.AddAssistantMessage(refusal);
            await _chatHistory.SaveMessageAsync(userId, ChatMessageRole.Assistant, refusal);

            return refusal;
        }

        chatHistory.AddUserMessage(question);

        string answer;
        try
        {
            // A copy, never the cached instance. Automatic function calling appends the
            // assistant tool_calls message and each tool result to whatever history it is
            // given, and this one is a process-lifetime singleton. Letting it accumulate
            // that bookkeeping would grow every prompt without bound, and a failure
            // part-way through invocation would strand a tool_calls message with no
            // matching tool result — a shape Azure OpenAI rejects, poisoning every later
            // turn for that user until the process restarts.
            var draft = await GenerateDraftAsync(new ChatHistory(chatHistory), cancellationToken);

            var review = await _responseGuard.ReviewAsync(
                question, draft, _groundingContext.Snapshot(), cancellationToken);

            answer = review.Content;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The in-memory history already holds the user turn; leaving it without a
            // matching assistant turn would desync it from the database for the rest of
            // the process lifetime.
            _logger.LogError(ex, "Failed to produce an answer for user {UserId}.", userId);

            // Outside Development this is the same text the response guard uses when it
            // rejects an answer, which makes an infrastructure failure indistinguishable
            // from a refusal. Locally, surface the actual exception — the alternative is
            // guessing from a generic apology.
            answer = _environment.IsDevelopment()
                ? $"{_guardOptions.UnverifiedMessage}\n\n[dev] {ex.GetType().Name}: {ex.Message}"
                : _guardOptions.UnverifiedMessage;
        }

        // Store what the customer was actually shown, not the pre-verification draft, so
        // the next turn reasons from the same text the customer saw.
        chatHistory.AddAssistantMessage(answer);
        await _chatHistory.SaveMessageAsync(userId, ChatMessageRole.Assistant, answer);

        return answer;
    }

    private async Task<string> GenerateDraftAsync(ChatHistory chatHistory, CancellationToken cancellationToken)
    {
        // Cloned per request: plugins are scoped, and the filter captures this request's
        // tool results only.
        var scopedKernel = _kernel.Clone();

        scopedKernel.ImportPluginFromObject(_productsPlugin);
        scopedKernel.ImportPluginFromObject(_cartPlugin);
        scopedKernel.ImportPluginFromObject(_couponsPlugin);
        scopedKernel.ImportPluginFromObject(_checkoutPlugin);
        scopedKernel.ImportPluginFromObject(_webSearchPlugin);

        scopedKernel.AutoFunctionInvocationFilters.Add(_groundingFilter);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        var chatService = scopedKernel.GetRequiredService<IChatCompletionService>();

        var response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            scopedKernel,
            cancellationToken);

        return response.Content ?? string.Empty;
    }

    /// <summary>
    /// Cuts the verified answer into delivery-sized pieces on whitespace, so the widget
    /// still fills in progressively and words are never split across chunks.
    /// </summary>
    private static IEnumerable<string> SplitForDelivery(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var buffer = new StringBuilder();

        foreach (var token in Tokenize(text))
        {
            buffer.Append(token);

            if (buffer.Length >= StreamChunkSize)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString();
        }
    }

    /// <summary>Splits into words with their trailing whitespace attached.</summary>
    private static IEnumerable<string> Tokenize(string text)
    {
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                continue;
            }

            // Absorb the whole whitespace run so newlines survive intact.
            var end = i;
            while (end < text.Length && char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            yield return text[start..end];
            start = end;
            i = end - 1;
        }

        if (start < text.Length)
        {
            yield return text[start..];
        }
    }
}
