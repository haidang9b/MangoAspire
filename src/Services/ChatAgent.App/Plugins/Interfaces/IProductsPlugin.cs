namespace ChatAgent.App.Plugins.Interfaces;

public interface IProductsPlugin
{
    Task<IEnumerable<ProductDto>?> GetAllProductsAsync();
    Task<IEnumerable<ProductDto>?> SearchProductsAsync(string searchTerm);
    Task<ProductDto?> GetProductByIdAsync(Guid productId);
    Task<string> RefreshProductCacheAsync();
}
