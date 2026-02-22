using Mango.Core.Pagination;
using Microsoft.EntityFrameworkCore;
using Products.API.Dtos;

namespace Products.API.Features.Products;

public class GetProducts
{
    public class Query : IQuery<PaginatedItems<ProductDto>>
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? CatalogTypeId { get; set; }

        internal class Handler(ProductDbContext dbContext) : IRequestHandler<Query, ResultModel<PaginatedItems<ProductDto>>>
        {
            public async Task<ResultModel<PaginatedItems<ProductDto>>> HandleAsync(Query request, CancellationToken cancellationToken)
            {
                var query = dbContext.Products
                    .AsNoTracking()
                    .AsQueryable();

                // Filter by CatalogTypeId if provided
                if (request.CatalogTypeId.HasValue)
                {
                    query = query.Where(x => x.CatalogTypeId == request.CatalogTypeId.Value);
                }

                query = query.OrderBy(x => x.Name);

                var totalCount = await query.CountAsync(cancellationToken);

                var data = await query
                    .Paginate(request.PageIndex, request.PageSize)
                    .Select(x => new ProductDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Price = x.Price,
                        CategoryName = x.CategoryName,
                        CatalogTypeId = x.CatalogTypeId,
                        Description = x.Description,
                        ImageUrl = x.ImageUrl,
                        Stock = x.AvailableStock
                    }).ToListAsync(cancellationToken);

                var result = new PaginatedItems<ProductDto>(
                    request.PageIndex,
                    request.PageSize,
                    totalCount,
                    data);

                return ResultModel<PaginatedItems<ProductDto>>.Create(result);
            }
        }
    }
}

