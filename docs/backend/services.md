# Backend Services Architecture

The back-end microservices of MangoAspire are fundamentally built on **.NET 10** utilizing a **Vertical Slice Architecture**.

## Vertical Slice Architecture
Instead of separating code technically by layers (e.g., Controllers, Services, Repositories), code is grouped by feature. 

Each feature (e.g., `CreateProduct`, `GetOrderById`) typically contains its own:
- Command/Query definition.
- Request Handler.
- Validation logic.
- DTOs.

This maximizes code cohesion and guarantees that changes to one feature won't inadvertently break another.

## MediatR and CQRS
We heavily use the **MediatR** library to implement the Command Query Responsibility Segregation (CQRS) pattern. HTTP endpoints do not contain business logic; they immediately dispatch a request to its corresponding handler.

## Workflow Orchestration
`Mango.Orchestrators` implements long-running saga flows for cross-service business processes. It owns a dedicated `sagaorchestratorsdb` database, consumes integration events from RabbitMQ, and coordinates downstream service interactions without tightly coupling those services to one another.

```csharp
// Example structure using C# 14 Primary Constructors
public record CreateProductCommand(string Name, decimal Price) : IRequest<ResultModel<Guid>>;

internal class Handler(ProductDbContext dbContext) : IRequestHandler<CreateProductCommand, ResultModel<Guid>>
{
    public async Task<ResultModel<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Business logic here
    }
}
```

## Caching

Services cache through a single abstraction, `ICacheManager` (`Mango.Core.Caching`), rather than taking a direct dependency on `IMemoryCache` or `IDistributedCache`.

The implementation, `HybridCacheManager` (`Mango.Infrastructure.Caching`), delegates to .NET's `HybridCache`. Reads are served from an in-process (L1) cache and fall through to a distributed (L2) cache when one is registered, and concurrent misses for the same key are collapsed into a single factory invocation — so a cold key under load produces one database round-trip, not one per request.

### Registration

```csharp
services.AddCacheManager();          // in-process only
services.AddCacheManager(options => { /* HybridCacheOptions */ });
```

Registering an `IDistributedCache` (for example Redis) adds the L2 layer with **no change to calling code**. `ICacheManager` is registered as a singleton.

### Contract

`GetOrCreateAsync` has two overloads. The `TState` overload passes state through to the factory so the callback can stay `static` and avoid allocating a closure per call — prefer it on hot paths.

```csharp
private static readonly CacheEntryOptions CacheOptions = new() { Expiration = TimeSpan.FromHours(1) };

var catalogTypes = await cacheManager.GetOrCreateAsync(
    CacheKey,
    dbContext,                                   // state
    static (db, ct) => new ValueTask<List<CatalogTypeDto>>(
        db.CatalogTypes.AsNoTracking()
          .OrderBy(x => x.Type)
          .Select(x => new CatalogTypeDto { Id = x.Id, Type = x.Type })
          .ToListAsync(ct)),
    CacheOptions,
    cancellationToken: cancellationToken);
```

Eviction is available per key (`RemoveAsync`), in batches, or by tag (`RemoveByTagAsync`). Tags let a whole family of entries be dropped in one call.

`CacheEntryOptions` exposes `Expiration` (overall) and `LocalExpiration` (L1 only). When `LocalExpiration` is omitted it defaults to `Expiration`; when both are null no options are passed through and `HybridCache` defaults apply.

### Current consumers

| Consumer | Key / tag | TTL | Why |
| :--- | :--- | :--- | :--- |
| `Products.API` — `GetCatalogTypes` | `CatalogTypes` | 1 hour | Small, near-static reference list read on most catalog requests. |
| `Identity.API` / `OpenIdentity.App` — `UserProfileCache` | `identity:user-profile:{userId}`, tagged `identity:user-profiles` | 5 minutes | The profile service is called on every token issuance and userinfo request, and each call otherwise costs several database round-trips (user, claims, roles, role claims). |

`UserProfileCache` stores a flattened `UserProfileSnapshot` because `System.Security.Claims.Claim` does not round-trip through JSON. Claims and roles are projected into a serializable `ClaimSnapshot` record. The TTL is deliberately short: role and claim changes are only picked up when the entry expires or is explicitly invalidated via `InvalidateAsync(userId)`, or `InvalidateAllAsync()` after a role-wide change.

## Resilience and Observability
All external HTTP communications and inter-service dependencies are decorated with Polly resilience pipelines (Retries, Circuit Breakers) managed through generic `.NET Aspire Service Defaults`.
Additionally, all logs, traces, and metrics are instrumented out-of-the-box with **OpenTelemetry**.
