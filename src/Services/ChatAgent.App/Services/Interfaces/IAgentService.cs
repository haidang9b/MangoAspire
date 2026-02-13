using ChatAgent.App.Dtos;

namespace ChatAgent.App.Services.Interfaces;

public interface IAgentService
{
    IAsyncEnumerable<string> ChatStreamingAsync(string userId, PromptRequestDto promptRequest);
}
