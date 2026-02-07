using Mango.Core.Exceptions;

namespace Products.API.Features.Products;

public class UpdateProduct
{
    public class Command : ICommand<bool>
    {
        public Guid Id { get; set; }

        public required string Name { get; set; }

        public decimal Price { get; set; }

        public required string Description { get; set; }

        public required string CategoryName { get; set; }

        public required string ImageUrl { get; set; }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Id).NotEmpty();
                RuleFor(x => x.Name).NotEmpty();
                RuleFor(x => x.Price).NotEmpty();
                RuleFor(x => x.Description).NotEmpty();
                RuleFor(x => x.CategoryName).NotEmpty();
                RuleFor(x => x.ImageUrl).NotEmpty();
            }
        }

        internal class Handler(ProductDbContext dbContext) : IRequestHandler<Command, ResultModel<bool>>
        {
            public async Task<ResultModel<bool>> Handle(Command request, CancellationToken cancellationToken)
            {
                var product = await dbContext.Products.FindAsync([request.Id], cancellationToken: cancellationToken)
                    ?? throw new DataVerificationException("Product not found");

                product.Name = request.Name;
                product.Price = request.Price;
                product.Description = request.Description;
                product.CategoryName = request.CategoryName;
                product.ImageUrl = request.ImageUrl;

                await dbContext.SaveChangesAsync(cancellationToken);

                return ResultModel<bool>.Create(true);
            }
        }
    }
}
