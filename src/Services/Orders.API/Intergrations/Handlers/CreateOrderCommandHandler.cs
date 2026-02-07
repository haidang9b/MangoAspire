using EventBus.Abstractions;
using Mango.Events.Orders;
using MediatR;
using Orders.API.Features.Orders;

namespace Orders.API.Intergrations.Handlers;

public class CreateOrderCommandHandler(
    ISender mediator,
    ILogger<CreateOrderCommandHandler> logger
) : IIntegrationEventHandler<CreateOrderCommand>
{
    public async Task HandleAsync(CreateOrderCommand @event)
    {
        logger.LogInformation("Handling CreateOrderCommand: {CorrelationId}", @event.CorrelationId);

        await mediator.Send(new CreateOrder.Command
        {
            CorrelationId = @event.CorrelationId,
            Event = @event.Event
        });
    }
}
