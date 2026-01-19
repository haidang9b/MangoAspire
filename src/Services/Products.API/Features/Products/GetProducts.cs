using Mango.Core.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Products.API.Data;
using Products.API.Dtos;

namespace Products.API.Features.Products;

public class GetProducts
{
    public class Query : IQuery<IEnumerable<ProductDto>>
    {
        internal class Handler(ProductDbContext dbContext) : IRequestHandler<Query, ResultModel<IEnumerable<ProductDto>>>
        {
            public async Task<ResultModel<IEnumerable<ProductDto>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var data = await dbContext.Products
                    .AsNoTracking()
                    .Select(x => new ProductDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Price = x.Price,
                        CategoryName = x.CategoryName,
                        Description = x.Description,
                        ImageUrl = x.ImageUrl
                    }).ToListAsync();

                return ResultModel<IEnumerable<ProductDto>>.Create(data);
            }
        }
    }
}
