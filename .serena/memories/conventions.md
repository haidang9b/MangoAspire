# Conventions

Authoritative sources: `AGENTS.md`, `.agent/CODING_CONVENTIONS.md`, `.agent/API_PROJECT_STRUCTURE.md`, `.agent/rules/`, `.editorconfig`.

## Architecture (backend)
- **Vertical Slice Architecture** — organize by feature slice under `Features/[FeatureName]/`. NOT Clean/Onion/N-tier layers.
- **No Repository pattern** — use `DbContext` directly inside Mediator handlers. (Exception: `Mango.Orchestrators` intentionally uses `SagaRepository`.)
- Each feature file = Command/Query + nested `Handler` + `Validator`.
- API project layout: `Data/` (DbContext, EntityTypeConfigurations, Migrations), `Entities/`, `Dtos/`, `Features/`, `Routes/`, `Extensions/`, `ExceptionHandlers/`, `IntegrationEvents/{Events,Handlers}/`.

## Program.cs pattern (keep < 15 lines)
`builder.AddApiDefaults()` → `app.UseApiPipeline()` → `await app.MigrateDatabaseAsync()`.
Wire DI/pipeline via extension methods in `Extensions/` (`WebApplicationBuilderExtensions`, `IServiceCollectionExtensions`, `WebApplicationExtensions`).

## Routes
- C# 14 **extension blocks**: `extension(WebApplication app) { public RouteGroupBuilder MapXApi() {...} }`.
- RESTful nouns, **kebab-case** segments. `GET /api/products/{id}`, not `/api/product/get-product/{id}`.
- Route handlers stay thin — delegate to Mediator; map results with `Results.Ok/Created/...`.

## Responses & errors
- Handlers return `ResultModel<T>` (from `Mango.Core`).
- Null checks: `... ?? throw new DataVerificationException("msg")`.
- All business/validation failures throw `DataVerificationException`; **never** return `ResultModel.Error` — the central `GlobalExceptionHandler` (+ `AddProblemDetails`) formats responses.

## Mediator pipeline order
`LoggingBehavior<,>` → `ValidationBehavior<,>` → `TxBehavior<,>`.

## Data access
- Read paths: `.AsNoTracking()` + `.Select(x => new Dto{...})` projection. Load full entities only for mutation.
- Explicit transactions only for multiple `SaveChangesAsync` or external side effects.
- `PerformanceInterceptor` for slow-query monitoring.

## Naming
- Commands/queries descriptive (`GetProductById`, `CreateCart`). Handlers nested in feature class.
- DB tables plural. Constants PascalCase.
- Tests: `MethodName_When_Behavior_Then_ExpectedResult`.

## Testing
- xUnit + Moq + Shouldly, AAA structure. Isolate db/files/network via mocking/in-memory. Verify side effects with Moq `Verify(...)`.

## Frontend
- TS strict, no `any`. API calls in `src/api/`, consumed via hooks. Reuse existing contexts (`AuthContext`, cart/theme/notification). Prefer `@/` alias imports and `import type`.
