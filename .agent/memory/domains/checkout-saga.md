# Checkout Saga (`Mango.Orchestrators`)

Orchestration-based saga (**not** choreography) coordinating a distributed checkout transaction
across `Orders.API`, `Products.API` and `Payments.API` over RabbitMQ.
Human-facing doc: `docs/CHECKOUT_SAGA.md`.

## Components (`src/Services/Mango.Orchestrators/`)

- `Sagas/CheckoutSagaOrchestrator.cs` (+ `ICheckoutSagaOrchestrator`) — the coordinator; publishes
  commands and reacts to events.
- `Entities/CheckoutSagaState.cs` — persisted state (`EntityBase<Guid>`): `Id` (= the
  `CorrelationId`), `CartId`, `OrderId?`, `UserId`, `ContextData` (JSON of `CartCheckedOutEvent`),
  `StatusId` (`OrderStatus` enum), `UpdatedDate`.
- `Repositories/SagaRepository.cs` — upsert persistence to `SagaDbContext` (Postgres
  `sagaorchestratorsdb`). This service intentionally uses a repository, unlike the rest of the
  codebase.
- `IntegrationHandlers/*` — thin adapters mapping each subscribed event to one orchestrator method.
  Subscriptions are wired in `Program.cs`.

## Flow

`CartCheckedOutEvent` → `CreateOrderCommand` → `OrderCreatedEvent` → `ReserveProductStockCommand`
→ `StockReservedEvent` → `CreatePaymentCommand` → `PaymentSucceededEvent` → `CompleteOrderCommand`
(status Completed).

Correlation is carried by the saga `Id`, propagated as `CorrelationId` on every command and event.

## Compensation

- Stock reservation fails → `CancelOrderCommand` → status Failed.
- Payment fails → `ReleaseProductStockCommand` + `CancelOrderCommand` → status Failed.

## Known doc drift

The "Saga State Persistence" section of `docs/CHECKOUT_SAGA.md` shows a `SagaState` class with
`PaymentId` / `CartItemsJson` that do **not** match the real `CheckoutSagaState` (which has
`ContextData` and no `PaymentId`). Trust the code, not that section.
