using ChatAgent.App.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace ChatAgent.App.Guards;

/// <summary>
/// Minimal chat helper shared by both guards: one system prompt, one user message, one
/// JSON reply.
/// </summary>
/// <remarks>
/// Execution settings are left at their defaults on purpose. Reasoning deployments such as
/// gpt-5.x reject <c>temperature</c> and rename <c>max_tokens</c>, so setting either would
/// make the guards fail on exactly the models most likely to be configured here.
/// </remarks>
public class GuardChatClient
{
    /// <summary>
    /// Service key for an optional second chat deployment dedicated to guard calls,
    /// registered when <c>AIAgent:Guard:ModelId</c> differs from the main model.
    /// </summary>
    public const string GuardServiceKey = "guard";

    private readonly Kernel _kernel;
    private readonly GuardOptions _options;
    private readonly ILogger<GuardChatClient> _logger;

    public GuardChatClient(Kernel kernel, IOptions<AIAgentConfiguration> options, ILogger<GuardChatClient> logger)
    {
        _kernel = kernel;
        _options = options.Value.Guard;
        _logger = logger;
    }

    /// <returns>The model's raw reply, or null if the call failed.</returns>
    /// <remarks>Virtual so guard behaviour can be tested without a live deployment.</remarks>
    public virtual async Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        // A guard is a small, fast call sitting on the customer's critical path, so it gets its
        // own budget rather than inheriting the turn's. Timing out here lands on the existing
        // "no usable verdict" path, so it needs no new branch in either guard.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.GuardTimeoutSeconds));

        try
        {
            var chatService = ResolveChatService();

            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userPrompt);

            // No kernel passed: guards must never be able to invoke tools themselves.
            var response = await chatService.GetChatMessageContentAsync(
                history,
                executionSettings: null,
                kernel: null,
                cancellationToken: timeoutCts.Token);

            return response.Content;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller gave up, or the client disconnected. Not ours to swallow.
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Guard model call timed out after {Seconds}s.", _options.GuardTimeoutSeconds);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Guard model call failed.");
            return null;
        }
    }

    private IChatCompletionService ResolveChatService()
    {
        var guardService = _kernel.Services.GetKeyedService<IChatCompletionService>(GuardServiceKey);
        return guardService ?? _kernel.GetRequiredService<IChatCompletionService>();
    }

    /// <summary>
    /// Pulls a JSON object out of a model reply. Models wrap JSON in prose or code fences
    /// often enough that parsing the raw string alone is not reliable.
    /// </summary>
    public static JsonElement? ExtractJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(raw[start..(end + 1)]);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
