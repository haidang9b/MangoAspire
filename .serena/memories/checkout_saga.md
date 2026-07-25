# Checkout Saga (Mango.Orchestrators)

Orchestration-based saga (NOT choreography) coordinating a distributed checkout transaction across Orders.API, Products.API, Payments.API over RabbitMQ. Doc: `docs/CHECKOUT_SAGA.md`.

## Components (`src/Services/Mango.Orchestrators/`)
- `Sagas/CheckoutSagaOrchestrator.cs` (+ `ICheckoutSagaOrchestrator`) — the coordinator; publishes commands, reacts to events.
- `Entities/CheckoutSagaState.cs` — persisted state (`EntityBase<Guid>`): `Id` (= CorrelationId), `CartId`, `OrderId?`, `UserId`, `ContextData` (JSON of `CartCheckedOutEvent`), `StatusId` (`OrderStatus` enum), `UpdatedDate`.
- `Repositories/SagaRepository.cs` — upsert persistence to `SagaDbContext` (Postgres `sagaorchestratorsdb`). This service intentionally uses a repository (unlike the rest of the codebase).
- `IntegrationHandlers/*` — thin adapters mapping each subscribed event → one orchestrator method. Subscriptions wired in `Program.cs`.

## Flow
Start (`CartCheckedOutEvent`) → `CreateOrderCommand` → `OrderCreatedEvent` → `ReserveProductStockCommand` → `StockReservedEvent` → `CreatePaymentCommand` → `PaymentSucceededEvent` → `CompleteOrderCommand` (status Completed).
Correlation via saga `Id` carried as `CorrelationId` on every command/event.

## Compensation
- Stock reservation fails → `CancelOrderCommand` → status Failed.
- Payment fails → `ReleaseProductStockCommand` + `CancelOrderCommand` → status Failed.

## KNOWN DOC DRIFT
`docs/CHECKOUT_SAGA.md` "Saga State Persistence" section shows a `SagaState` class with `PaymentId`/`CartItemsJson` that do NOT match the real `CheckoutSagaState` (which has `ContextData`, no `PaymentId`). Doc is stale there.
