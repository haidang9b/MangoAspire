namespace Mango.Orchestrators.IntegrationHandlers;

public class PaymentFailedEventHandler(
    ICheckoutSagaOrchestrator orchestrator,
    ILogger<PaymentFailedEventHandler> logger
) : IIntegrationEventHandler<PaymentFailedEvent>
{
    public async Task HandleAsync(PaymentFailedEvent @event)
    {
        logger.LogInformation("Handling PaymentFailedEvent: {CorrelationId}, Reason: {Reason}",
            @event.CorrelationId, @event.Reason);
        await orchestrator.OnPaymentFailedAsync(@event);
    }
}
