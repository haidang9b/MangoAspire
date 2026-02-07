namespace Mango.SagaOrchestrators.IntegrationHandlers;

public class StockReservedEventHandler(
    ICheckoutSagaOrchestrator orchestrator,
    ILogger<StockReservedEventHandler> logger
) : IIntegrationEventHandler<StockReservedEvent>
{
    public async Task HandleAsync(StockReservedEvent @event)
    {
        logger.LogInformation("Handling StockReservedEvent: {CorrelationId}", @event.CorrelationId);
        await orchestrator.OnStockReservedAsync(@event);
    }
}
