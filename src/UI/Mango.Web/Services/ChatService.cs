using Mango.Core.Pagination;
using Mango.Web.Models;

namespace Mango.Web.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;

    public ChatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async IAsyncEnumerable<PromptResponse> SendPromptAsync(PromptRequest promptRequest)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(promptRequest)
        };


        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var stream = response.Content.ReadFromJsonAsAsyncEnumerable<PromptResponse>();
        if (stream != null)
        {
            await foreach (var item in stream)
            {
                if (item != null)
                {
                    yield return item;
                }
            }
        }
    }

    public async Task<PaginatedItems<ChatHistoryMessage>> GetChatHistoryAsync(int pageSize = 10, int pageIndex = 1)
    {
        var response = await _httpClient.GetAsync($"/api/chat-histories?pageSize={pageSize}&pageIndex={pageIndex}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PaginatedItems<ChatHistoryMessage>>();
        return result ?? new PaginatedItems<ChatHistoryMessage>(pageIndex, pageSize, 0, new List<ChatHistoryMessage>());
    }
}
