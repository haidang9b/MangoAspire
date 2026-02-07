# Checkout Saga Workflow

This document describes the workflow of the Checkout Saga, orchestrated by `CheckoutSagaOrchestrator`.

## Saga State Machine

```mermaid
stateDiagram-v2
    [*] --> Started : StartAsync (CartCheckedOutEvent)
    Started --> CreateOrderCommand
    
    state "Order Service" as OrderService
    CreateOrderCommand --> OrderService
    OrderService --> OrderCreated : OrderCreatedEvent (OnOrderCreatedAsync)
    
    OrderCreated --> ReserveProductStockCommand
    
    state "Stock Service" as StockService
    ReserveProductStockCommand --> StockService
    
    StockService --> StockReserved : StockReservedEvent (OnStockReservedAsync)
    StockService --> StockReserveFailed : StockReservationFailedEvent (OnStockFailedAsync)
    
    StockReserved --> CreatePaymentCommand
    
    state "Payment Service" as PaymentService
    CreatePaymentCommand --> PaymentService
    
    PaymentService --> PaymentSucceeded : PaymentSucceededEvent (OnPaymentSucceededAsync)
    PaymentService --> PaymentFailed : PaymentFailedEvent (OnPaymentFailedAsync)
    
    PaymentSucceeded --> CompleteOrderCommand
    CompleteOrderCommand --> OrderService
    OrderService --> Completed : Order Status = Completed
    
    PaymentFailed --> ReleaseProductStockCommand
    PaymentFailed --> CancelOrderCommand
    
    StockReserveFailed --> CancelOrderCommand
    
    Completed --> [*]
    PaymentFailed --> [*] : Status = Failed
    StockReserveFailed --> [*] : Status = Failed
```

## Flow Description

1.  **Start**: The Saga starts when a `CartCheckedOutEvent` is received. It publishes a `CreateOrderCommand`.
2.  **Order Creation**: The Order Service creates the order and publishes `OrderCreatedEvent`.
3.  **Stock Reservation**: The Saga requests stock reservation via `ReserveProductStockCommand`.
    -   **Success**: `StockReservedEvent` triggers payment creation.
    -   **Failure**: `StockReservationFailedEvent` triggers `CancelOrderCommand` and ends the saga as `Failed`.
4.  **Payment**: The Saga requests payment via `CreatePaymentCommand`.
    -   **Success**: `PaymentSucceededEvent` triggers `CompleteOrderCommand` to finalize the order, then marks the saga as `Completed`.
    -   **Failure**: `PaymentFailedEvent` triggers `ReleaseProductStockCommand` and `CancelOrderCommand`, ending the saga as `Failed`.
