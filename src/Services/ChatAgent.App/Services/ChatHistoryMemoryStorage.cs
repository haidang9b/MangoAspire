using ChatAgent.App.Data.Entities;
using ChatAgent.App.Data.Enums;
using ChatAgent.App.Data.EntityTypeConfigurations;
using ChatAgent.App.Guards;
using ChatAgent.App.Guards.Input;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

namespace ChatAgent.App.Services;

public class ChatHistoryMemoryStorage : IChatHistoryMemoryStorage
{
    private readonly ConcurrentDictionary<string, ChatHistory> _chatHistoryCache = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _systemMessage;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public ChatHistoryMemoryStorage(
        IServiceScopeFactory scopeFactory,
        IOptions<AIAgentConfiguration> config)
    {
        _scopeFactory = scopeFactory;
        _systemMessage = config.Value.SystemMessage;
    }

    public async Task<ChatHistory> GetChatHistoryAsync(string userId)
    {
        // Check if already cached
        if (_chatHistoryCache.TryGetValue(userId, out var cachedHistory))
            return cachedHistory;

        // Use lock to prevent multiple simultaneous loads for the same user
        await _loadLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_chatHistoryCache.TryGetValue(userId, out cachedHistory))
                return cachedHistory;

            // Load from database
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(_systemMessage);

            // Create scope to resolve scoped repository
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IChatHistoryRepository>();

            // Load recent messages from database (first page)
            var result = await repository.GetRecentMessagesAsync(userId, 10, 1);

            foreach (var message in result.Data)
            {
                if (message.Role == ChatMessageRole.User)
                    chatHistory.AddUserMessage(message.Content);
                else if (message.Role == ChatMessageRole.Assistant)
                    chatHistory.AddAssistantMessage(message.Content);
            }

            _chatHistoryCache[userId] = chatHistory;
            return chatHistory;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task SaveMessageAsync(
        string userId,
        ChatMessageRole role,
        string content,
        ReviewVerdictKind? reviewVerdict = null)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Role = role,
            // Last line of defence on the column width. Customer messages are already bounded by
            // the input guard, but an assistant answer is whatever the model produced.
            Content = PromptFormatValidator.Truncate(content, ChatMessageConfiguration.MaxContentLength),
            ReviewVerdict = reviewVerdict,
            CreatedAt = DateTime.UtcNow
        };

        // Create scope to resolve scoped repository
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IChatHistoryRepository>();
        await repository.AddMessageAsync(message);
    }

    public async Task ClearHistoryAsync(string userId)
    {
        // Create scope to resolve scoped repository
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IChatHistoryRepository>();
        await repository.ClearHistoryAsync(userId);
        _chatHistoryCache.TryRemove(userId, out _);
    }
}
