using Mango.Events.Orders;
using Products.API.Features.Products;

namespace Products.API.IntegrationHandlers;

public class ReserveProductStockCommandHandler(
    ISender mediator,
    ILogger<ReserveProductStockCommandHandler> logger
) : IIntegrationEventHandler<ReserveProductStockCommand>
{
    public async Task HandleAsync(ReserveProductStockCommand @event)
    {
        logger.LogInformation("Handling ReserveProductStockCommand: {CorrelationId}", @event.CorrelationId);

        await mediator.Send(new ReserveProductStock.Command
        {
            Id = @event.Id,
            CorrelationId = @event.CorrelationId,
            Items = @event.Items.Select(i => new ReserveProductStock.Command.StockItem(i.ProductId, i.Quantity))
        });
    }
}
