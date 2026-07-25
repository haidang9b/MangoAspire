# Infrastructure & Database Schema

The infrastructure logic centers heavily around cloud-native orchestration managed locally through **.NET Aspire**.
For production, the Aspire Manifest guarantees smooth Azure Developer CLI (`azd`) deployments across AKS/Container Apps.

## Database-per-Service
The system explicitly abides by the database-per-service pattern. Sharing relational databases across service boundaries is strictly prohibited.
All relational stores are backed by **PostgreSQL** and modeled with Entity Framework Core (EF Core) via Code-First migrations.

- **`identitydb`**: Schema dedicated to user accounts, roles, claims, and OAuth grants.
- **`openidentitydb`**: Independent schema for the OpenIddict-based authentication service.
- **`productdb`**: Master schema for Products and Catalog Types.
- **`shoppingcartdb`**: Extremely transient database for storing cart session data.
- **`orderdb`**: Master transaction log managing order states.
- **`coupondb`**: Stores static promotional data.
- **`sagaorchestratorsdb`**: Saga state for `Mango.Orchestrators`.
- **`chatagentdb`**: Conversation history for `ChatAgent.App`.

Only one of `identitydb` / `openidentitydb` is in use per run, determined by the `IdentityType` switch — see the [Architecture Overview](../architecture/overview.md#identity-provider-switch). Both databases are declared in `Mango.AppHost`, but only the selected provider's project is started and referenced.

The PostgreSQL container runs with `-c wal_level=logical`. Debezium's `pgoutput` plugin requires logical decoding, and `wal_level` cannot be set from SQL — `postgresql.conf` lives inside the data volume, so it is passed as a server argument instead.

## Caching Tier
Read-heavy paths sit behind `ICacheManager`, backed by `HybridCache`. Today this is an in-process (L1) cache per service instance with no external dependency; adding an `IDistributedCache` (for example Redis) introduces the shared L2 tier without touching handler code. See [Backend Services](../backend/services.md#caching).

Note the operational consequence of the current L1-only setup: cached entries are per-instance, so eviction (including `RemoveByTagAsync`) only affects the instance that handles the call. Scaling a service out means an entry may persist on sibling instances until its TTL expires.

## Change Data Capture
To circumvent direct database joins, the `ShoppingCart` service consumes a local read-model of product details (name, price, imageUrl). This model is populated via **Debezium**, which monitors the `Products` database's write-ahead log. When a product changes, Debezium publishes an event to RabbitMQ, eventually updating the downstream cart schema.

### Handling Replication Lag
Because the product table in `shoppingcartdb` is an eventually-consistent replica, a product can exist in `productdb` before it has been replicated downstream. `UpsertCart` therefore checks the local replica before inserting a cart detail:

```csharp
var productExists = await dbContext.Products
    .AsNoTracking()
    .AnyAsync(p => p.Id == request.Cart.ProductId, cancellationToken);

if (!productExists)
{
    throw new DataVerificationException($"Product '{request.Cart.ProductId}' was not found.");
}
```

This turns a not-yet-replicated product into a business error surfaced as ProblemDetails by the `GlobalExceptionHandler`, rather than a foreign key violation thrown from `SaveChangesAsync`.

## Event Broker
RabbitMQ is the default local event broker, abstracted heavily through a core Event Bus interfaces (e.g., `IMessageBus` or MassTransit configuration).
This allows decoupled microservices to coordinate complex long-running operations (like submitting an order and fulfilling payment) fully asynchronously.
