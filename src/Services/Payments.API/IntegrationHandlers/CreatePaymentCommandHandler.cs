using Payments.API.Features;

namespace Payments.API.IntegrationHandlers;

public class CreatePaymentCommandHandler(
    ISender mediator,
    ILogger<CreatePaymentCommandHandler> logger
) : IIntegrationEventHandler<CreatePaymentCommand>
{
    public async Task HandleAsync(CreatePaymentCommand @event)
    {
        logger.LogInformation("Handling CreatePaymentCommand: {CorrelationId}, OrderId: {OrderId}",
            @event.CorrelationId, @event.OrderId);

        await mediator.Send(new ProcessPayment.Command
        {
            CorrelationId = @event.CorrelationId,
            OrderId = @event.OrderId,
            OrderTotal = @event.OrderTotal,
            CardNumber = @event.CardNumber,
            CVV = @event.CVV,
            ExpiryMonthYear = @event.ExpiryMonthYear,
            Email = @event.Email
        });
    }
}
