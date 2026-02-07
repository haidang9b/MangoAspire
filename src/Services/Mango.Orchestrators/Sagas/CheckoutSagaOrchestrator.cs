using System.Text.Json;

namespace Mango.SagaOrchestrators.Sagas;

/// <summary>
/// Orchestrates the Checkout Saga workflow.
/// Coordinates order creation, stock reservation, and payment processing.
/// </summary>
public class CheckoutSagaOrchestrator(
    IEventBus eventBus,
    ISagaRepository sagaRepository,
    ILogger<CheckoutSagaOrchestrator> logger
) : ICheckoutSagaOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #region Saga Entry Point

    public async Task StartAsync(CartCheckedOutEvent @event)
    {
        logger.LogInformation("Starting checkout saga for Cart: {CartId}, User: {UserId}",
            @event.CartId, @event.UserId);

        var saga = new CheckoutSagaState
        {
            Id = Guid.NewGuid(),
            CartId = @event.CartId,
            UserId = @event.UserId,
            StatusId = OrderStatus.Started,
            ContextData = SerializeContext(@event),
        };

        await sagaRepository.SaveAsync(saga);

        logger.LogInformation("Saga {SagaId} created. Publishing CreateOrderCommand", saga.Id);
        await eventBus.PublishAsync(new CreateOrderCommand(saga.Id, saga.CartId, @event));
    }

    #endregion

    #region Happy Path Handlers

    public async Task OnOrderCreatedAsync(OrderCreatedEvent @event)
    {
        logger.LogInformation("Order created for Saga: {SagaId}, Order: {OrderId}",
            @event.CorrelationId, @event.OrderId);

        var saga = await GetSagaAsync(@event.CorrelationId);
        saga.StatusId = OrderStatus.OrderCreated;
        saga.OrderId = @event.OrderId;
        await sagaRepository.SaveAsync(saga);

        var context = DeserializeContext(saga.ContextData);
        var stockItems = context?.CartDetails.Select(cd => new ReserveProductStockCommand.StockItem
        {
            ProductId = cd.ProductId,
            Quantity = cd.Count
        }) ?? [];

        logger.LogInformation("Saga {SagaId}: Publishing ReserveProductStockCommand", saga.Id);
        await eventBus.PublishAsync(new ReserveProductStockCommand
        {
            CorrelationId = saga.Id,
            Items = stockItems
        });
    }

    public async Task OnStockReservedAsync(StockReservedEvent @event)
    {
        logger.LogInformation("Stock reserved for Saga: {SagaId}", @event.CorrelationId);

        var saga = await GetSagaAsync(@event.CorrelationId);
        saga.StatusId = OrderStatus.StockReserved;
        await sagaRepository.SaveAsync(saga);

        var context = DeserializeContext(saga.ContextData)
            ?? throw new InvalidOperationException($"Saga {saga.Id}: Context data is null or invalid");

        logger.LogInformation("Saga {SagaId}: Publishing CreatePaymentCommand for Order: {OrderId}",
            saga.Id, saga.OrderId);

        await eventBus.PublishAsync(new CreatePaymentCommand(
            saga.Id,
            saga.OrderId.GetValueOrDefault(),
            context.OrderTotal,
            context.CardNumber,
            context.CVV,
            context.ExpiryMonthYear ?? "",
            context.Email
        ));
    }

    public async Task OnPaymentSucceededAsync(PaymentSucceededEvent @event)
    {
        logger.LogInformation("Payment succeeded for Saga: {SagaId}", @event.CorrelationId);

        var saga = await GetSagaAsync(@event.CorrelationId);
        saga.StatusId = OrderStatus.Completed;
        await sagaRepository.SaveAsync(saga);

        // Publish command to complete the order
        await eventBus.PublishAsync(new CompleteOrderCommand(saga.Id, saga.OrderId.GetValueOrDefault()));

        logger.LogInformation("Saga {SagaId}: Completed successfully", saga.Id);
    }

    #endregion

    #region Compensation Handlers

    public async Task OnStockFailedAsync(StockReservationFailedEvent @event)
    {
        logger.LogWarning("Stock reservation failed for Saga: {SagaId}, Reason: {Reason}",
            @event.CorrelationId, @event.Reason);

        var saga = await GetSagaAsync(@event.CorrelationId);
        saga.StatusId = OrderStatus.Failed;
        await sagaRepository.SaveAsync(saga);

        logger.LogInformation("Saga {SagaId}: Publishing CancelOrderCommand", saga.Id);
        await eventBus.PublishAsync(new CancelOrderCommand(saga.Id, saga.OrderId.GetValueOrDefault()));
    }

    public async Task OnPaymentFailedAsync(PaymentFailedEvent @event)
    {
        logger.LogWarning("Payment failed for Saga: {SagaId}, Reason: {Reason}",
            @event.CorrelationId, @event.Reason);

        var saga = await GetSagaAsync(@event.CorrelationId);
        var context = DeserializeContext(saga.ContextData);

        // Compensate: Release reserved stock
        var stockItems = context?.CartDetails.Select(cd => new ReleaseProductStockCommand.StockItem
        {
            ProductId = cd.ProductId,
            Quantity = cd.Count
        }) ?? [];

        logger.LogInformation("Saga {SagaId}: Publishing ReleaseProductStockCommand", saga.Id);
        await eventBus.PublishAsync(new ReleaseProductStockCommand
        {
            CorrelationId = saga.Id,
            Items = stockItems
        });

        // Compensate: Cancel order
        logger.LogInformation("Saga {SagaId}: Publishing CancelOrderCommand", saga.Id);
        await eventBus.PublishAsync(new CancelOrderCommand(saga.Id, saga.OrderId.GetValueOrDefault()));

        saga.StatusId = OrderStatus.Failed;
        await sagaRepository.SaveAsync(saga);

        logger.LogInformation("Saga {SagaId}: Failed - compensation completed", saga.Id);
    }

    #endregion

    #region Private Helpers

    private async Task<CheckoutSagaState> GetSagaAsync(Guid correlationId)
    {
        var saga = await sagaRepository.GetAsync(correlationId);
        if (saga == null)
        {
            logger.LogError("Saga not found: {CorrelationId}", correlationId);
            throw new InvalidOperationException($"Saga not found: {correlationId}");
        }
        return saga;
    }

    private static CartCheckedOutEvent? DeserializeContext(string data)
        => JsonSerializer.Deserialize<CartCheckedOutEvent>(data, JsonOptions);

    private static string SerializeContext(CartCheckedOutEvent @event)
        => JsonSerializer.Serialize(@event, JsonOptions);

    #endregion
}
