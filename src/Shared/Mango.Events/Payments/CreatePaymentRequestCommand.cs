using EventBus.Events;

namespace Mango.Events.Payments;

public record CreatePaymentRequestCommand : IntegrationEvent
{
    public Guid OrderId { get; set; }

    public required string Name { get; set; }

    public required string CardNumber { get; set; }

    public required string CVV { get; set; }

    public string? ExpiryMonthYear { get; set; }

    public decimal OrderTotal { get; set; }

    public required string Email { get; set; }
}
