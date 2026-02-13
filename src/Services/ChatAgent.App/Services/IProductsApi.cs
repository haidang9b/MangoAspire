using Mango.Core.Domain;
using Refit;

namespace ChatAgent.App.Services;

public interface IProductsApi
{
    [Get("/api/products")]
    Task<ResultModel<PaginatedItemsDto<ProductDto>>> GetProductsAsync(int pageIndex = 1, int pageSize = 50, int? catalogTypeId = null);
}


public class PaginatedItemsDto<T> where T : class
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public long Count { get; set; }
    public IEnumerable<T> Data { get; set; } = [];

    public int TotalPages => (int)Math.Ceiling(Count / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}



//public record ProductDto
//{
//    public Guid Id { get; set; }

//    public required string Name { get; set; }

//    public decimal Price { get; set; }

//    public required string Description { get; set; }

//    public required string CategoryName { get; set; }

//    public int? CatalogTypeId { get; set; }

//    public required string ImageUrl { get; set; }

//    public int Stock { get; set; }
//}

