using EventBus.Abstractions;
using FluentValidation;
using Mango.Core.Domain;
using Mango.Events.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Products.API.Data;

namespace Products.API.Features.Products;

public class ReserveProductStock
{
    public class Command : ICommand<bool>
    {
        public required Guid CorrelationId { get; init; }

        public required IEnumerable<StockItem> Items { get; init; }

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
        IEventBus eventBus,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var itemsList = request.Items.ToList();
            var productIds = itemsList.Select(i => i.ProductId).ToList();

            logger.LogInformation("Reserving stock for {Count} products", productIds.Count);

            // Batch query - single database call
            var products = await dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            // Check all products exist
            var missingProducts = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missingProducts.Any())
            {
                var missing = string.Join(", ", missingProducts);
                logger.LogWarning("Products not found: {ProductIds}", missing);
                await eventBus.PublishAsync(new StockReservationFailedEvent(
                    request.CorrelationId,
                    Guid.Empty,
                    $"Products not found: {missing}"
                ));
                return ResultModel<bool>.Create(false, true, $"Products not found: {missing}");
            }

            // Check stock availability
            var itemsDict = itemsList.ToDictionary(i => i.ProductId, i => i.Quantity);
            foreach (var product in products)
            {
                var requiredQty = itemsDict[product.Id];
                if (product.AvailableStock < requiredQty)
                {
                    logger.LogWarning("Insufficient stock for product {ProductId}: required {Required}, available {Available}",
                        product.Id, requiredQty, product.AvailableStock);
                    await eventBus.PublishAsync(new StockReservationFailedEvent(
                        request.CorrelationId,
                        Guid.Empty,
                        $"Insufficient stock for {product.Name}"
                    ));
                    return ResultModel<bool>.Create(false, true, $"Insufficient stock for {product.Name}");
                }
            }

            // Reserve stock
            foreach (var product in products)
            {
                product.AvailableStock -= itemsDict[product.Id];
                logger.LogInformation("Reserved {Quantity} of product {ProductId}", itemsDict[product.Id], product.Id);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await eventBus.PublishAsync(new StockReservedEvent(request.CorrelationId, Guid.Empty));

            return ResultModel<bool>.Create(true);
        }
    }
}
