using Mango.Core.Domain;
using Mango.Core.Exceptions;
using Mango.Services.Products.Data;
using MediatR;

namespace Mango.Services.Products.Features.Products;

public class DeleteProduct
{
    public record Command : ICommand<bool>
    {
        public Guid Id { get; set; }

        internal class Handler(ProductDbContext dbContext) : IRequestHandler<Command, ResultModel<bool>>
        {
            public async Task<ResultModel<bool>> Handle(Command request, CancellationToken cancellationToken)
            {
                var product = await dbContext.Products.FindAsync(request.Id)
                    ?? throw new DataVerificationException("Product not found");

                dbContext.Remove(product);
                await dbContext.SaveChangesAsync(cancellationToken);

                return ResultModel<bool>.Create(true);
            }
        }
    }
}
