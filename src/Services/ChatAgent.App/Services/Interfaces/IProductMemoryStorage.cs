namespace ChatAgent.App.Services.Interfaces;

public interface IProductMemoryStorage
{
    Task<IEnumerable<ProductDto>> GetProductsAsync(bool forceRefresh = false);
    Task<ProductDto?> GetProductByIdAsync(Guid productId);
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);
    Task ForceRefreshAsync();
}
