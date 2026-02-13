using ChatAgent.App.Dtos;
using Mango.Core.Auth;
using System.Runtime.CompilerServices;

namespace ChatAgent.App.Routes;

public static class ChatRoute
{
    public static RouteGroupBuilder MapChatRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/chat", HandleChatPrompt)
        .WithName("ChatPrompt");

        group.MapGet("/chat-histories", GetChatHistory)
        .WithName("GetChatHistory");

        return group;
    }

    private static async IAsyncEnumerable<PromptResponseDto> HandleChatPrompt(
        PromptRequestDto request,
        IAgentService agentService,
        ICurrentUserContext currentUserContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {

        await foreach (var chunk in agentService.ChatStreamingAsync(
            currentUserContext.UserId ?? throw new UnauthorizedAccessException(),
            request
        ))
        {
            if (cancellationToken.IsCancellationRequested) break;

            yield return new PromptResponseDto { Content = chunk };
        }
    }

    private static async Task<IResult> GetChatHistory(
        IChatHistoryRepository repository,
        ICurrentUserContext currentUserContext,
        int pageSize = 10,
        int pageIndex = 1)
    {
        var userId = currentUserContext.UserId ?? throw new UnauthorizedAccessException();
        var result = await repository.GetRecentMessagesAsync(userId, pageSize, pageIndex);

        return Results.Ok(result);
    }
}
