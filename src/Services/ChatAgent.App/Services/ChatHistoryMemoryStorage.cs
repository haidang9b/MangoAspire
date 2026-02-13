using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

namespace ChatAgent.App.Services;

public class ChatHistoryMemoryStorage
{
    private readonly ConcurrentDictionary<string, ChatHistory> _chatHistory = new();

    public ChatHistory GetChatHistory(string userId)
    {

        return _chatHistory.GetOrAdd(userId, _ =>
        {

            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage("""
                You are an AI restaurant ordering assistant.
                - Use GetProducts to find menu items.
                - Use AddToCart when user wants to order food.
                - Use ApplyVoucher when user provides discount code.
                Always confirm actions clearly.
                """);

            return chatHistory;
        });
    }
}
