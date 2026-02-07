using EventBus.Events;

namespace Mango.Events.Orders;

public record OrderCancelledEvent(Guid CorrelationId, Guid OrderId) : IntegrationEvent;

public record OrderCreatedEvent(Guid CorrelationId, Guid OrderId) : IntegrationEvent;

public record PaymentFailedEvent(Guid CorrelationId, string Reason) : IntegrationEvent;

public record PaymentSucceededEvent(Guid CorrelationId) : IntegrationEvent;

public record StockReservedEvent(Guid CorrelationId, Guid OrderId) : IntegrationEvent;

public record StockReservationFailedEvent(Guid CorrelationId, Guid OrderId, string Reason) : IntegrationEvent;


public record CreateOrderCommand(Guid CorrelationId, Guid CartId, CartCheckedOutEvent Event) : IntegrationEvent;

public record CancelOrderCommand(Guid CorrelationId, Guid OrderId, string Reason) : IntegrationEvent;

public record CompleteOrderCommand(Guid CorrelationId, Guid OrderId) : IntegrationEvent;

public record CreatePaymentCommand(Guid CorrelationId, Guid OrderId, decimal OrderTotal, string CardNumber, string CVV, string ExpiryMonthYear, string Email) : IntegrationEvent;


public record ReserveProductStockCommand : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public IEnumerable<StockItem> Items { get; init; } = [];

    public record StockItem
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
    }
}
public record ReleaseProductStockCommand : IntegrationEvent
{
    public Guid CorrelationId { get; init; }

    public IEnumerable<StockItem> Items { get; init; } = [];

    public record StockItem
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
    }
}
