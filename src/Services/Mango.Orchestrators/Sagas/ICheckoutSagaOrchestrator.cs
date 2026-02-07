using Mango.Events.Orders;

namespace Mango.SagaOrchestrators.Sagas;

/// <summary>
/// Interface for the Checkout Saga Orchestrator.
/// Orchestrates the checkout workflow across multiple services.
/// </summary>
public interface ICheckoutSagaOrchestrator
{
    /// <summary>
    /// Starts a new checkout saga when a cart is checked out.
    /// </summary>
    Task StartAsync(CartCheckedOutEvent @event);

    /// <summary>
    /// Handles the order created event, proceeding to stock reservation.
    /// </summary>
    Task OnOrderCreatedAsync(OrderCreatedEvent @event);

    /// <summary>
    /// Handles successful stock reservation, proceeding to payment.
    /// </summary>
    Task OnStockReservedAsync(StockReservedEvent @event);

    /// <summary>
    /// Handles stock reservation failure, triggering compensating actions.
    /// </summary>
    Task OnStockFailedAsync(StockReservationFailedEvent @event);

    /// <summary>
    /// Handles successful payment, completing the saga.
    /// </summary>
    Task OnPaymentSucceededAsync(PaymentSucceededEvent @event);

    /// <summary>
    /// Handles payment failure, triggering compensating actions.
    /// </summary>
    Task OnPaymentFailedAsync(PaymentFailedEvent @event);
}
