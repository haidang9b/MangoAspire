using ChatAgent.App.Dtos;

namespace ChatAgent.App.Plugins.Interfaces;

public interface IProductsPlugin
{
    Task<IEnumerable<ProductSummaryDto>> GetAllProductsAsync();
    Task<IEnumerable<ProductSummaryDto>> SearchProductsAsync(string searchTerm);
    Task<ProductSummaryDto?> GetProductByIdAsync(Guid productId);
    Task<IEnumerable<string>> GetCategoriesAsync();
    Task<string> SearchStoreInfoAsync(string question);
}
