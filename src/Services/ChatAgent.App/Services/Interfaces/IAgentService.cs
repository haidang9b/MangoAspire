using ChatAgent.App.Dtos;

namespace ChatAgent.App.Services.Interfaces;

public interface IAgentService
{
    /// <summary>
    /// Answers one customer turn. The text is generated and verified in full before the
    /// first chunk is yielded, so the customer never sees unverified content.
    /// </summary>
    IAsyncEnumerable<string> ChatStreamingAsync(
        string userId,
        PromptRequestDto promptRequest,
        CancellationToken cancellationToken = default);
}
