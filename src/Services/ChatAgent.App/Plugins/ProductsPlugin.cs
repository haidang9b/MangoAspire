using ChatAgent.App.Data;
using ChatAgent.App.Data.Enums;
using ChatAgent.App.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ChatAgent.App.Plugins;

/// <summary>
/// Menu and store-knowledge tools, served entirely from this service's own database.
/// </summary>
/// <remarks>
/// Products and categories are replicated from Products.API by Debezium CDC, so a chat
/// turn never blocks on a cross-service HTTP call, and search is semantic rather than a
/// substring scan. Search goes through <see cref="IKnowledgeSearchService"/>, which uses
/// embeddings when they are configured and Postgres full-text search when they are not.
/// </remarks>
public class ProductsPlugin : IProductsPlugin
{
    /// <summary>Kept small so retrieved context stays well inside the model's window.</summary>
    private const int SearchResultLimit = 8;

    private const int StoreInfoResultLimit = 4;

    private readonly ChatAgentDbContext _dbContext;
    private readonly IKnowledgeSearchService _searchService;

    public ProductsPlugin(ChatAgentDbContext dbContext, IKnowledgeSearchService searchService)
    {
        _dbContext = dbContext;
        _searchService = searchService;
    }

    [KernelFunction]
    [Description("Get the complete menu of all available products. Use this when the user asks to see the full menu, all dishes, or wants to browse everything available.")]
    public async Task<IEnumerable<ProductSummaryDto>> GetAllProductsAsync()
    {
        return await _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.CategoryName)
            .ThenBy(p => p.Name)
            .Select(p => new ProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryName = p.CategoryName,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
            })
            .ToListAsync();
    }

    [KernelFunction]
    [Description("Search for dishes by meaning, not just keywords. Use this when the user describes what they want (e.g. 'something spicy and vegetarian', 'a light lunch', 'pasta'), asks about ingredients, or looks for a particular item.")]
    public async Task<IEnumerable<ProductSummaryDto>> SearchProductsAsync(
        [Description("What the user is looking for, in their own words")] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllProductsAsync();
        }

        var hits = await _searchService.SearchAsync(
            searchTerm,
            [VectorSourceType.Product],
            SearchResultLimit);

        if (hits.Count == 0)
        {
            return [];
        }

        // Hydrate from the read-model rather than answering out of the indexed text, so
        // prices and names come from the replicated row and cannot drift from the index.
        var ids = hits
            .Select(h => Guid.TryParse(h.SourceId, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new ProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryName = p.CategoryName,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
            })
            .ToListAsync();

        // Preserve relevance order, which the IN-clause query does not guarantee.
        var ranking = ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        return [.. products.OrderBy(p => ranking.TryGetValue(p.Id, out var rank) ? rank : int.MaxValue)];
    }

    [KernelFunction]
    [Description("Get detailed information about one specific dish using its unique ID. Use this when you already have a product ID from a previous search.")]
    public async Task<ProductSummaryDto?> GetProductByIdAsync(
        [Description("The unique identifier (GUID) of the product")] Guid productId)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new ProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryName = p.CategoryName,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
            })
            .FirstOrDefaultAsync();
    }

    [KernelFunction]
    [Description("List the menu categories available at the restaurant. Use this when the user asks what kinds of food are served or wants to narrow down the menu.")]
    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        return await _dbContext.ProductCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync();
    }

    [KernelFunction]
    [Description("Look up facts about the restaurant itself: opening hours, address and directions, phone and email, delivery and pickup, reservations, payment methods, refunds and cancellations, allergens, loyalty points and coupon rules, catering, and privacy. ALWAYS use this instead of guessing store details.")]
    public async Task<string> SearchStoreInfoAsync(
        [Description("The user's question about the store, in their own words")] string question)
    {
        var hits = await _searchService.SearchAsync(
            question,
            [VectorSourceType.KnowledgeChunk],
            StoreInfoResultLimit);

        if (hits.Count == 0)
        {
            return "No store information was found for that question.";
        }

        return string.Join("\n\n---\n\n", hits.Select(h => h.Content));
    }
}
