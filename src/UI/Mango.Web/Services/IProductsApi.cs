using Mango.Core.Domain;
using Mango.Web.Models;
using Refit;

namespace Mango.Web.Services;

public interface IProductsApi
{
    [Get("/api/products")]
    Task<ResultModel<PaginatedItemsDto<ProductDto>>> GetProductsAsync(int pageIndex = 0, int pageSize = 10);

    [Get("/api/products/{id}")]
    Task<ResultModel<ProductDto>> GetProductByIdAsync(Guid id);

    [Post("/api/products")]
    Task<ResultModel<Guid>> CreateProductAsync([Body] ProductDto productDto);

    [Put("/api/products")]
    Task<ResultModel<bool>> UpdateProductAsync([Body] ProductDto productDto);

    [Delete("/api/products/{id}")]
    Task<ResultModel<bool>> DeleteProductAsync(Guid id);
}
