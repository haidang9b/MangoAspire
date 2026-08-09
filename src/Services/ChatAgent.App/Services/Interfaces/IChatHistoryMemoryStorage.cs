using ChatAgent.App.Data.Enums;
using ChatAgent.App.Guards;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatAgent.App.Services.Interfaces;

public interface IChatHistoryMemoryStorage
{
    Task<ChatHistory> GetChatHistoryAsync(string userId);

    /// <param name="reviewVerdict">
    /// What the response guard decided, for assistant messages that reached it. Optional so
    /// customer messages and refusals can omit it.
    /// </param>
    Task SaveMessageAsync(
        string userId,
        ChatMessageRole role,
        string content,
        ReviewVerdictKind? reviewVerdict = null);

    Task ClearHistoryAsync(string userId);
}
