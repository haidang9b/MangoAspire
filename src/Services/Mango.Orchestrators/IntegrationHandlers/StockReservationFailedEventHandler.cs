namespace Mango.Orchestrators.IntegrationHandlers;

public class StockReservationFailedEventHandler(
    ICheckoutSagaOrchestrator orchestrator,
    ILogger<StockReservationFailedEventHandler> logger
) : IIntegrationEventHandler<StockReservationFailedEvent>
{
    public async Task HandleAsync(StockReservationFailedEvent @event)
    {
        logger.LogInformation("Handling StockReservationFailedEvent: {CorrelationId}, Reason: {Reason}",
            @event.CorrelationId, @event.Reason);
        await orchestrator.OnStockFailedAsync(@event);
    }
}
