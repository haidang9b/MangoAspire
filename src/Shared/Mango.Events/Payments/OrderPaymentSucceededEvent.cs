using EventBus.Events;

namespace Mango.Events.Payments;

public record OrderPaymentSucceededEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }

    public required string Email { get; set; }
}
