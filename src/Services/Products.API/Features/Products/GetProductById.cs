using Mango.Core.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Products.API.Data;
using Products.API.Dtos;

namespace Products.API.Features.Products;

public class GetProductById
{
    public class Query : IQuery<ProductDto>
    {
        public Guid ProductId { get; set; }

        internal class Handler(ProductDbContext dbContext) : IRequestHandler<Query, ResultModel<ProductDto>>
        {
            public async Task<ResultModel<ProductDto>> Handle(Query request, CancellationToken cancellationToken)
            {
                var product = await dbContext.Products
                    .Where(x => x.Id == request.ProductId)
                    .Select(x => new ProductDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Price = x.Price,
                        CategoryName = x.CategoryName,
                        Description = x.Description,
                        ImageUrl = x.ImageUrl
                    }).FirstOrDefaultAsync();

                return ResultModel<ProductDto>.Create(product);
            }
        }
    }
}
