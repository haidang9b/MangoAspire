using Mango.Core.Pagination;
using Microsoft.EntityFrameworkCore;
using Products.API.Dtos;

namespace Products.API.Features.Products;

public class GetProducts
{
    public class Query : IQuery<PaginatedItems<ProductDto>>
    {
        public required ProductSearchRequestDto Options { get; set; }

        internal class Handler(ProductDbContext dbContext) : IRequestHandler<Query, ResultModel<PaginatedItems<ProductDto>>>
        {
            public async Task<ResultModel<PaginatedItems<ProductDto>>> HandleAsync(Query request, CancellationToken cancellationToken)
            {
                var options = request.Options;
                var query = dbContext.Products
                    .AsNoTracking()
                    .AsQueryable();

                // Filter by Search if provided
                if (!string.IsNullOrWhiteSpace(options.Search))
                {
                    query = query.Where(x => x.Name.Contains(options.Search));
                }

                // Filter by CatalogTypeId if provided
                if (options.CatalogTypeId.HasValue)
                {
                    query = query.Where(x => x.CatalogTypeId == options.CatalogTypeId.Value);
                }

                query = query.OrderBy(x => x.Name);

                var totalCount = await query.CountAsync(cancellationToken);

                var data = await query
                    .Paginate(options.PageIndex, options.PageSize)
                    .Select(x => new ProductDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Price = x.Price,
                        CategoryName = x.CatalogType!.Type,
                        CatalogTypeId = x.CatalogTypeId,
                        Description = x.Description,
                        ImageUrl = x.ImageUrl,
                        Stock = x.AvailableStock
                    }).ToListAsync(cancellationToken);

                var result = new PaginatedItems<ProductDto>(
                    options.PageIndex.GetValueOrDefault(),
                    options.PageSize.GetValueOrDefault(),
                    totalCount,
                    data);

                return ResultModel<PaginatedItems<ProductDto>>.Create(result);
            }
        }
    }
}

