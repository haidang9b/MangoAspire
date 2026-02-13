using ChatAgent.App.Models;

namespace ChatAgent.App.Services;

public interface IAgentService
{
    IAsyncEnumerable<string> ChatStreamingAsync(string userId, PromptRequest promptRequest);
}
