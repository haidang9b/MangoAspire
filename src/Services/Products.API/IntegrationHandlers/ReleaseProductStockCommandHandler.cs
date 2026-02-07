using EventBus.Abstractions;
using Mango.Events.Orders;
using MediatR;
using Products.API.Features.Products;

namespace Products.API.IntegrationHandlers;

public class ReleaseProductStockCommandHandler(
    ISender mediator,
    ILogger<ReleaseProductStockCommandHandler> logger
) : IIntegrationEventHandler<ReleaseProductStockCommand>
{
    public async Task HandleAsync(ReleaseProductStockCommand @event)
    {
        logger.LogInformation("Handling ReleaseProductStockCommand: {CorrelationId}", @event.CorrelationId);

        await mediator.Send(new ReleaseProductStock.Command
        {
            CorrelationId = @event.CorrelationId,
            Items = @event.Items.Select(i => new ReleaseProductStock.Command.StockItem(i.ProductId, i.Quantity))
        });
    }
}
