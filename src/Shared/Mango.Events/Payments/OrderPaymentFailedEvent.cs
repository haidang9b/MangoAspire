using EventBus.Events;

namespace Mango.Events.Payments;

public record OrderPaymentFailedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }

    public OrderPaymentFailedEvent(Guid orderId)
    {
        OrderId = orderId;
    }
}
