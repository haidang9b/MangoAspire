using ChatAgent.App.Data.Enums;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatAgent.App.Services.Interfaces;

public interface IChatHistoryMemoryStorage
{
    Task<ChatHistory> GetChatHistoryAsync(string userId);
    Task SaveMessageAsync(string userId, ChatMessageRole role, string content);
    Task ClearHistoryAsync(string userId);
}
