using Mango.Core.Pagination;
using Mango.Web.Models;
using System.Text.Json;

namespace Mango.Web.Services;

public class ChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        // The endpoint streams newline-delimited JSON, one PromptResponse per line.
        // ReadFromJsonAsAsyncEnumerable cannot be used here: it expects a single JSON
        // array, which does not surface an item until the array closes.
        var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var item = JsonSerializer.Deserialize<PromptResponse>(line, JsonOptions);
            if (item != null)
            {
                yield return item;
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
