using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatAgent.App.Services.Interfaces;

public interface IChatHistoryMemoryStorage
{
    ChatHistory GetChatHistory(string userId);
}
