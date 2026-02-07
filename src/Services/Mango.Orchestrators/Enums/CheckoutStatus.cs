namespace Mango.SagaOrchestrators.Enums;

public enum OrderStatus
{
    Started,

    CartCreated,

    OrderCreated,

    StockReserved,

    StockReservedFailed,

    PaymentInitiated,

    Failed,

    Completed
}
