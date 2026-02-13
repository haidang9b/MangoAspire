namespace ChatAgent.App.Plugins;

public class ProductsPlugin : IProductsPlugin
{
    private readonly IProductMemoryStorage _productMemory;

    public ProductsPlugin(IProductMemoryStorage productMemory)
    {
        _productMemory = productMemory;
    }

    [KernelFunction]
    [Description("Get the complete menu of all available products. Use this when the user asks to see the full menu, all dishes, or wants to browse everything available.")]
    public async Task<IEnumerable<ProductDto>?> GetAllProductsAsync()
    {
        return await _productMemory.GetProductsAsync();
    }

    [KernelFunction]
    [Description("Search for products by name, description, or category. Use this when the user asks about specific dishes, ingredients, food types (e.g., 'pasta', 'spicy', 'vegetarian'), or wants to find particular items.")]
    public async Task<IEnumerable<ProductDto>?> SearchProductsAsync(
        [Description("Search keyword or phrase (e.g., dish name, ingredient, category, or description)")] string searchTerm)
    {
        return await _productMemory.SearchProductsAsync(searchTerm);
    }

    [KernelFunction]
    [Description("Get detailed information about a specific product using its unique ID. Use this when you have a product ID and need to fetch its complete details (price, description, stock, etc.).")]
    public async Task<ProductDto?> GetProductByIdAsync(
        [Description("The unique identifier (GUID) of the product")] Guid productId)
    {
        return await _productMemory.GetProductByIdAsync(productId);
    }

    [KernelFunction]
    [Description("Force refresh the product catalog cache to fetch the latest menu items from the database. Use this only when explicitly requested by the user or when products seem outdated.")]
    public async Task<string> RefreshProductCacheAsync()
    {
        await _productMemory.ForceRefreshAsync();
        return "Product catalog has been refreshed successfully.";
    }
}
