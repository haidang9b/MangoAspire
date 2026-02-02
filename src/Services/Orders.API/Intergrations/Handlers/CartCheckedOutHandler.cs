using EventBus.Abstractions;
using Mango.Events.Orders;
using MediatR;
using Orders.API.Features.Orders;

namespace Orders.API.Intergrations.Handlers;

public class CartCheckedOutHandler(ISender mediator) : IIntegrationEventHandler<CartCheckedOutEvent>
{
    public async Task HandleAsync(CartCheckedOutEvent @event)
    {
        await mediator.Send(new CreateOrder.Command { Event = @event });
    }
}
