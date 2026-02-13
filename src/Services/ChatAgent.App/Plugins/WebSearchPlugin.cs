using System.Text.Json.Serialization;

namespace ChatAgent.App.Plugins;

public class WebSearchPlugin : IWebSearchPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _searchApiKey;

    public WebSearchPlugin(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("BingSearch");
        _searchApiKey = configuration["WebSearch:ApiKey"] ?? "";
    }

    [KernelFunction]
    [Description("Search the web for information about dishes, ingredients, recipes, cooking methods, or food-related topics. Use this ONLY when the information is NOT available in the menu (e.g., nutritional facts, recipe details, cooking tips, food origins, or general culinary knowledge).")]
    public async Task<string> SearchWebAsync(
        [Description("Search query about food, dish, ingredient, recipe, or culinary topic (e.g., 'carbonara recipe', 'is pho healthy', 'origin of tiramisu')")] string query)
    {
        if (string.IsNullOrWhiteSpace(_searchApiKey))
            return "Web search is not configured.";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}&count=3");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _searchApiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BingSearchResponse>();

            if (result?.WebPages?.Value == null || result.WebPages.Value.Count == 0)
                return $"No results found for: {query}";

            var results = result.WebPages.Value.Take(3)
                .Select(r => $"**{r.Name}**\n{r.Snippet}\nSource: {r.Url}")
                .ToList();

            return string.Join("\n\n---\n\n", results);
        }
        catch (Exception ex)
        {
            return $"Search failed: {ex.Message}";
        }
    }
}

// Bing Search API response models
public class BingSearchResponse
{
    [JsonPropertyName("webPages")]
    public WebPages? WebPages { get; set; }
}

public class WebPages
{
    [JsonPropertyName("value")]
    public List<SearchResult> Value { get; set; } = [];
}

public class SearchResult
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = "";
}
