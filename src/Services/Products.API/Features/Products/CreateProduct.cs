namespace Products.API.Features.Products;

public class CreateProduct
{
    public class Command : ICommand<Guid>
    {
        public required string Name { get; set; }

        public decimal Price { get; set; }

        public required string Description { get; set; }

        public required string CategoryName { get; set; }

        public int? CatalogTypeId { get; set; }

        public required string ImageUrl { get; set; }

        public int Stock { get; set; }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Name).NotEmpty();
                RuleFor(x => x.Price).GreaterThan(0);
                RuleFor(x => x.Description).NotEmpty();
                RuleFor(x => x.CategoryName).NotEmpty();
                RuleFor(x => x.ImageUrl).NotEmpty();
                RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
            }
        }
        internal class Handler(ProductDbContext dbContext) : IRequestHandler<Command, ResultModel<Guid>>
        {
            public async Task<ResultModel<Guid>> Handle(Command request, CancellationToken cancellationToken)
            {
                var product = new Product
                {
                    Name = request.Name,
                    Price = request.Price,
                    Description = request.Description,
                    CategoryName = request.CategoryName,
                    CatalogTypeId = request.CatalogTypeId,
                    ImageUrl = request.ImageUrl,
                    AvailableStock = request.Stock
                };

                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync(cancellationToken);

                return ResultModel<Guid>.Create(product.Id);
            }
        }
    }
}
