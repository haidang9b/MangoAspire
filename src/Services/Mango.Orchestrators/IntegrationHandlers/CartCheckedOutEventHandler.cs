using EventBus.Abstractions;
using Mango.Events.Orders;
using Mango.SagaOrchestrators.Sagas;

namespace Mango.SagaOrchestrators.IntegrationHandlers;

public class CartCheckedOutEventHandler(
    ICheckoutSagaOrchestrator orchestrator,
    ILogger<CartCheckedOutEventHandler> logger
) : IIntegrationEventHandler<CartCheckedOutEvent>
{
    public async Task HandleAsync(CartCheckedOutEvent @event)
    {
        logger.LogInformation("Handling CartCheckedOutEvent: {CartId}", @event.CartId);
        await orchestrator.StartAsync(@event);
    }
}
