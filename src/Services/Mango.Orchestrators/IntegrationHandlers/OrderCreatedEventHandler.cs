namespace Mango.SagaOrchestrators.IntegrationHandlers;

public class OrderCreatedEventHandler(
    ICheckoutSagaOrchestrator orchestrator,
    ILogger<OrderCreatedEventHandler> logger
) : IIntegrationEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        logger.LogInformation("Handling OrderCreatedEvent: {OrderId}", @event.OrderId);
        await orchestrator.OnOrderCreatedAsync(@event);
    }
}
