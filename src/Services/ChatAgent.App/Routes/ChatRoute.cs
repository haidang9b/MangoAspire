using Mango.Core.Auth;
using System.Runtime.CompilerServices;

namespace ChatAgent.App.Routes;

public static class ChatRoute
{
    public static RouteGroupBuilder MapChatRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/chat", HandleChatPrompt)
        .WithName("ChatPrompt");

        return group;
    }

    private static async IAsyncEnumerable<PromptResponse> HandleChatPrompt(
        PromptRequest request,
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

            yield return new PromptResponse { Content = chunk };
        }
    }
}
