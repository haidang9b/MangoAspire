using ChatAgent.App.Guards.Input;
using ChatAgent.App.Guards.Untrusted;
using System.Text.Json.Serialization;

namespace ChatAgent.App.Plugins;

public class WebSearchPlugin : IWebSearchPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _searchApiKey;
    private readonly ILogger<WebSearchPlugin> _logger;

    public WebSearchPlugin(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WebSearchPlugin> logger)
    {
        _httpClient = httpClientFactory.CreateClient("BingSearch");
        _searchApiKey = configuration["WebSearch:ApiKey"] ?? "";
        _logger = logger;
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

            // Web pages are the least trustworthy input this agent has: anyone can publish one,
            // and ranking for a food query is a solved problem for whoever wants to. A result
            // carrying injection markers is dropped outright rather than neutralised, because
            // unlike a product description or a store document it has no value worth preserving.
            var results = result.WebPages.Value.Take(3)
                .Where(r => !PromptSecurityScanner.IsSuspicious($"{r.Name}\n{r.Snippet}"))
                .Select(r => string.Join(
                    '\n',
                    $"**{UntrustedText.Neutralize(r.Name)}**",
                    UntrustedText.Neutralize(r.Snippet),
                    $"Source: {UntrustedText.Neutralize(r.Url)}"))
                .ToList();

            if (results.Count == 0)
            {
                return $"No usable results found for: {query}";
            }

            return string.Join("\n\n---\n\n", results);
        }
        catch (Exception ex)
        {
            // The message itself is not returned: it lands in the model's context and from there
            // can be quoted to a customer, and an HTTP failure message can carry a URL, a header,
            // or an upstream error body.
            _logger.LogWarning(ex, "Web search failed for query {Query}.", query);
            return "Web search is unavailable right now.";
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
