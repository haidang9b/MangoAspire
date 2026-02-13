namespace Mango.Orchestrators.IntegrationHandlers;

public class PaymentSucceededEventHandler(
    ICheckoutSagaOrchestrator orchestrator,
    ILogger<PaymentSucceededEventHandler> logger
) : IIntegrationEventHandler<PaymentSucceededEvent>
{
    public async Task HandleAsync(PaymentSucceededEvent @event)
    {
        logger.LogInformation("Handling PaymentSucceededEvent: {CorrelationId}", @event.CorrelationId);
        await orchestrator.OnPaymentSucceededAsync(@event);
    }
}
