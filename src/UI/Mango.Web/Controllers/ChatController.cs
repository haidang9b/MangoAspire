using Mango.Web.Models;
using Mango.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Web.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SendMessage([FromBody] PromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Message cannot be empty.");
        }

        Response.Headers.Append("Content-Type", "text/event-stream");

        await foreach (var promptResponse in _chatService.SendPromptAsync(request))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(promptResponse);
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();
        }

        return new EmptyResult();
    }
}
