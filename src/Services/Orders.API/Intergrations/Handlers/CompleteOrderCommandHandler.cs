using Orders.API.Features.Orders;

namespace Orders.API.Intergrations.Handlers;

public class CompleteOrderCommandHandler(
    ISender mediator,
    ILogger<CompleteOrderCommandHandler> logger
) : IIntegrationEventHandler<CompleteOrderCommand>
{
    public async Task HandleAsync(CompleteOrderCommand @event)
    {
        logger.LogInformation("Handling CompleteOrderCommand: {OrderId}", @event.OrderId);

        await mediator.SendAsync(new CompleteOrder.Command
        {
            Id = @event.OrderId,
            CorrelationId = @event.CorrelationId,
            OrderId = @event.OrderId
        });
    }
}
