using EventBus.Abstractions;
using EventBus.Events;
using Mango.Events.Payments;
using Microsoft.Extensions.Options;
using Payments.API.Configurations;

namespace Payments.API.IntegrationHandlers;

public class CreatePaymentRequestCommandHandler(
    IEventBus eventBus,
    IOptionsMonitor<PaymentOptions> options,
    ILogger<CreatePaymentRequestCommandHandler> logger
) : IIntegrationEventHandler<CreatePaymentRequestCommand>
{
    public async Task HandleAsync(CreatePaymentRequestCommand @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        IntegrationEvent orderPaymentIntegrationEvent;
        if (options.CurrentValue.PaymentSucceeded)
        {
            orderPaymentIntegrationEvent = new OrderPaymentSucceededEvent
            {
                Email = @event.Email,
                OrderId = @event.OrderId
            };
        }
        else
        {
            orderPaymentIntegrationEvent = new OrderPaymentFailedEvent(@event.OrderId);
        }

        logger.LogInformation("Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", orderPaymentIntegrationEvent.Id, orderPaymentIntegrationEvent);

        await eventBus.PublishAsync(orderPaymentIntegrationEvent);
    }
}
