using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

namespace ChatAgent.App.Services;

public class ChatHistoryMemoryStorage : IChatHistoryMemoryStorage
{
    private readonly ConcurrentDictionary<string, ChatHistory> _chatHistory = new();
    private readonly string _systemMessage;

    public ChatHistoryMemoryStorage(IOptions<AIAgentConfiguration> config)
    {
        _systemMessage = config.Value.SystemMessage;
    }

    public ChatHistory GetChatHistory(string userId)
    {
        return _chatHistory.GetOrAdd(userId, _ =>
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(_systemMessage);
            return chatHistory;
        });
    }
}
