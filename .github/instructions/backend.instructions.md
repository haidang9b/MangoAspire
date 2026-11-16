---
applyTo: "src/**/*.cs"
---

<!-- GENERATED from .claude/ by scripts/sync-agent-harness.ps1 - do not edit directly -->

# Backend conventions (.NET 11 / C# 15)

## Architecture
- **Vertical Slice Architecture:** one feature per file in `Features/<Area>/` (query or command class with a nested `internal` `Handler`), routes in `Routes/`. No Domain/Application/Service layers.
- **No repository layer:** handlers use the `DbContext` directly. The one deliberate exception is `Mango.Orchestrators` (saga repository).
- **No cross-slice calls:** a feature never uses another feature's handler or logic; share infrastructure only.
- **Mediator:** requests go through the in-house mediator (`src/Shared/Mediator`). Routes call `ISender` (`SendAsync`); handlers implement `IRequestHandler<,>` with `HandleAsync`.
- **Pipeline order:** register each behaviour as `IPipelineBehavior<,>` in this order: `IdentifiedBehavior` (services with idempotent commands), `LoggingBehavior`, `ValidationBehavior`, `TxBehavior`.
- **Templates** for new slices, routes, wiring and events: the `add-feature-slice` skill.

## Language
- File-scoped namespaces.
- Respect nullable reference types.
- Primary constructors for DI.
- `required` for mandatory init-only properties.
- C# 14 extension blocks for route mapping.

## Program.cs
- Keep it under 15 lines: `AddApiDefaults()`, `UseApiPipeline()`, `MigrateDatabaseAsync()`. Everything else lives in `Extensions/`.

## Validation and errors
- Every command has a FluentValidation `AbstractValidator<T>` in its slice. Complex business checks may also run in the handler.
- Business and data-integrity failures throw `DataVerificationException`. Null checks after lookups use `?? throw new DataVerificationException("…")`.
- Never return an ad-hoc error `ResultModel` from a handler. `GlobalExceptionHandler` (registered via `AddGlobalExceptionHandler()`) turns exceptions into ProblemDetails.

## Responses and routes
- Handlers return `ResultModel<T>` (from `Mango.Core`). Endpoints map it with `Results.Ok(...)`, or `Results.Created(uri, result)` for creation.
- Routes are RESTful kebab-case nouns (`GET /api/products/{id}`, `POST /api/products`), never verbs like `/get-product`.
- Name commands and queries for their intent (`GetProductById`); table names are plural (`Products`, `CartHeaders`).

## Data access
- Reads use `AsNoTracking()` plus a `Select` projection to a DTO. Load full entities only to mutate them.
- Use explicit transactions only for multiple `SaveChangesAsync` calls or external side effects; `TxBehavior` covers the usual case.
- Use `PerformanceInterceptor` for slow-query monitoring.

## Configuration
- Package versions live only in `Directory.Packages.props` (central package management).
- Reference `Mango.ServiceDefaults`. PostgreSQL goes through Aspire's `AddNpgsqlDataSource()` / `EnrichNpgsqlDbContext<T>()`.
- Secrets come from environment variables or local-dev `appsettings.json`; never commit them.
