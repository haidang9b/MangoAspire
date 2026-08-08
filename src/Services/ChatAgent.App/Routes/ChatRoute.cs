using ChatAgent.App.Dtos;
using Mango.Core.Auth;
using System.Text.Json;

namespace ChatAgent.App.Routes;

public static class ChatRoute
{
    /// <summary>
    /// Web defaults give camelCase, so the payload is <c>{"content":"..."}</c> — which is
    /// what both front-ends read.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapChatRoutes(this RouteGroupBuilder group)
    {
        // The ApiScope policy was configured but never applied here. Without it the only
        // check was a throw inside the handler, which surfaces as a broken stream after
        // the response has started rather than a clean 401.
        group.MapPost("/chat", HandleChatPrompt)
        .WithName("ChatPrompt")
        .RequireAuthorization("ApiScope");

        group.MapGet("/chat-histories", GetChatHistory)
        .WithName("GetChatHistory")
        .RequireAuthorization("ApiScope");

        return group;
    }

    /// <summary>
    /// Streams the answer as newline-delimited JSON, one <see cref="PromptResponseDto"/>
    /// per line.
    /// </summary>
    /// <remarks>
    /// Written straight to the response body rather than returned as an
    /// <c>IAsyncEnumerable</c>: minimal APIs serialise that as a single JSON array
    /// (<c>[{...},{...}]</c>), which a client cannot parse until the array closes, so
    /// nothing renders incrementally. NDJSON gives each client a complete object per line,
    /// and the explicit flush is what actually pushes each one to the browser.
    /// </remarks>
    private static async Task HandleChatPrompt(
        PromptRequestDto request,
        IAgentService agentService,
        ICurrentUserContext currentUserContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = currentUserContext.UserId ?? throw new UnauthorizedAccessException();

        httpContext.Response.ContentType = "application/x-ndjson";

        // The token is threaded all the way to the model call, so a customer closing the
        // widget actually cancels the upstream request instead of leaving it running.
        await foreach (var chunk in agentService.ChatStreamingAsync(userId, request, cancellationToken))
        {
            var line = JsonSerializer.Serialize(new PromptResponseDto { Content = chunk }, JsonOptions);

            await httpContext.Response.WriteAsync(line + "\n", cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
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
