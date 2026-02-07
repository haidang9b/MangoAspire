using Orders.API.Features.Orders;

namespace Orders.API.Intergrations.Handlers;

public class CancelOrderCommandHandler(
    ISender mediator,
    ILogger<CancelOrderCommandHandler> logger
) : IIntegrationEventHandler<CancelOrderCommand>
{
    public async Task HandleAsync(CancelOrderCommand @event)
    {
        logger.LogInformation("Handling CancelOrderCommand: {OrderId}", @event.OrderId);

        await mediator.Send(new CancelOrder.Command
        {
            CorrelationId = @event.CorrelationId,
            OrderId = @event.OrderId
        });
    }
}
