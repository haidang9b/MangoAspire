# Tech Stack

## Backend

- **.NET 11** (`net11.0`), **C# 15** (compiler default) — extension blocks for route mapping,
  primary constructors, file-scoped namespaces.
- **Deviation — EF Core stays on 10.0.x while everything else is 11.x.** Npgsql's EF provider had no
  stable 11.x at the .NET 11 move (its `11.0.0-rc.3` pins EF Core to that exact RC), and
  `EFCore.NamingConventions` caps EF Core at `< 11.0.0`. Hold back together: `Microsoft.EntityFrameworkCore.*`,
  `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `MockQueryable.Moq` — their 11.x builds pull
  EF Core 11 transitively. Unblock condition in `.agent/tickets/DOTNET11-001/ticket.md` (blk-1).
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
