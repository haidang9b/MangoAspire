using Microsoft.EntityFrameworkCore;

namespace Products.API.Features.Products;

public class ReleaseProductStock
{
    public class Command : IIdentifiedCommand<bool>
    {
        public required Guid CorrelationId { get; init; }

        public required IEnumerable<StockItem> Items { get; init; }
        public Guid Id { get; set; }

        public bool CreateDefaultResponse()
        {
            return false;
        }

        public record StockItem(Guid ProductId, int Quantity);
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CorrelationId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty();
        }
    }

    internal class Handler(
        ProductDbContext dbContext,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            var itemsList = request.Items.ToList();
            var productIds = itemsList.Select(i => i.ProductId).ToList();

            logger.LogInformation("Releasing stock for {Count} products", productIds.Count);

            // Batch query - single database call
            var products = await dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            var itemsDict = itemsList.ToDictionary(i => i.ProductId, i => i.Quantity);

            // Release stock
            foreach (var product in products)
            {
                if (itemsDict.TryGetValue(product.Id, out var quantity))
                {
                    product.AvailableStock += quantity;
                    logger.LogInformation("Released {Quantity} of product {ProductId}", quantity, product.Id);
                }
            }

            // Log missing products (non-blocking)
            var missingProducts = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missingProducts.Any())
            {
                logger.LogWarning("Products not found for release: {ProductIds}", string.Join(", ", missingProducts));
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return ResultModel<bool>.Create(true);
        }
    }
}
