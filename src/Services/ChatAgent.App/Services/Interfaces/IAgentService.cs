namespace ChatAgent.App.Services.Interfaces;

public interface IAgentService
{
    IAsyncEnumerable<string> ChatStreamingAsync(string userId, PromptRequest promptRequest);
}
