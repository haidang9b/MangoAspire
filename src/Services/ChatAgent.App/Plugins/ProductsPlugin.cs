namespace ChatAgent.App.Plugins;

public class ProductsPlugin : IProductsPlugin
{
    private readonly IProductMemoryStorage _productMemory;

    public ProductsPlugin(IProductMemoryStorage productMemory)
    {
        _productMemory = productMemory;
    }

    [KernelFunction]
    [Description("Get all available products")]
    public async Task<IEnumerable<ProductDto>?> GetAllProductsAsync()
    {
        return await _productMemory.GetProductsAsync();
    }

    [KernelFunction]
    [Description("Search for products by name or keyword. Use this when user asks about specific dishes, food items, or product details.")]
    public async Task<IEnumerable<ProductDto>?> SearchProductsAsync(
        [Description("Search keyword or product name")] string searchTerm)
    {
        return await _productMemory.SearchProductsAsync(searchTerm);
    }

    [KernelFunction]
    [Description("Get detailed information about a specific product by its ID")]
    public async Task<ProductDto?> GetProductByIdAsync(
        [Description("Product ID (GUID)")] Guid productId)
    {
        return await _productMemory.GetProductByIdAsync(productId);
    }

    [KernelFunction]
    [Description("Refresh the product catalog cache to get the latest products")]
    public async Task<string> RefreshProductCacheAsync()
    {
        await _productMemory.ForceRefreshAsync();
        return "Product catalog has been refreshed successfully.";
    }
}
