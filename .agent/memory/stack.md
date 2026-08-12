# Tech Stack

## Backend

- **.NET 10** (`net10.0`), **C# 14** — extension blocks for route mapping, primary constructors,
  file-scoped namespaces.
- Nullable reference types **enabled**; implicit usings enabled.
- **.NET Aspire** for orchestration and service discovery (`Mango.AppHost`,
  `Mango.ServiceDefaults`).
- **Minimal APIs** (no MVC controllers) + **Mediator** — a custom `src/Shared/Mediator`,
  MediatR-style — for request handling.
- **EF Core** with **Npgsql** (PostgreSQL). Aspire integrations: `AddNpgsqlDataSource`,
  `EnrichNpgsqlDbContext<T>`.
- **FluentValidation** for command/query validation.
- Messaging: **RabbitMQ** via `EventBus.RabbitMQ` (abstraction `IEventBus`; an Azure Service Bus
  alternative lives in `EventBus.ServiceBus`). **Debezium** for CDC — now on a replayable RabbitMQ
  stream log (`docs/CDC.md`).
- Identity: **Duende IdentityServer** (`Identity.API`) and **OpenIddict** (`OpenIdentity.App`, plus
  `OpenIddict.Quartz` and `Quartz`).
- Observability: Grafana + Prometheus + Loki alongside the Aspire dashboard
  (`docs/OBSERVABILITY.md`).

## Frontend (`src/UI/mango-ui`)

- **React 19 + Vite + TypeScript**, strict mode, no `any`.
- **pnpm** (`pnpm-lock.yaml`) — **not** npm.
- ESLint via `eslint.config.js`. **No test runner is configured.**
- Path alias `@/` (Vite + tsconfig).

## Dependency management

**Central Package Management** via `Directory.Packages.props`. Do not pin versions in individual
`.csproj` files. `Directory.Build.props` enables `WarningsAsErrors`, so a warning fails the build.
