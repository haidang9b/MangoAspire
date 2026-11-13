# Tech Stack

## Backend

- **.NET 11** (`net11.0`), **C# 15** (compiler default) — extension blocks for route mapping,
  primary constructors, file-scoped namespaces. C# 15 in use: collection-expression `with(...)`
  arguments and labeled `continue` (ChatAgent guards, CS15-001). **Deliberately not adopted:**
  `union` for `ResultModel<T>` (it is the JSON wire contract the SPA and `Mango.Web` read), and
  `closed` for `IntegrationEvent` (subtypes live in other assemblies, which `closed` forbids).
- **Deviation — EF Core stays on 10.0.x while everything else is 11.x.** Npgsql's EF provider had no
  stable 11.x at the .NET 11 move (its `11.0.0-rc.3` pins EF Core to that exact RC), and
  `EFCore.NamingConventions` caps EF Core at `< 11.0.0`. Hold back together: `Microsoft.EntityFrameworkCore.*`,
  `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `MockQueryable.Moq` — their 11.x builds pull
  EF Core 11 transitively. Unblock condition in `.agent/tickets/DOTNET11-001/ticket.md` (blk-1).
- Nullable reference types **enabled**; implicit usings enabled.
- **.NET Aspire 13.6.5** for orchestration and service discovery (`Mango.AppHost`,
  `Mango.ServiceDefaults`). The `Aspire.AppHost.Sdk` version in `Mango.AppHost.csproj` is a
  separate pin from `Directory.Packages.props` — it sat at 9.5.0 under 13.x packages until
  SEC-DEPS-002; bump both together.
- **Aspire dashboard is versioned through NuGet, deliberately.** The SDK auto-references
  `Aspire.Dashboard.Sdk.<rid>` (and DCP) at the `Aspire.Hosting.AppHost` version because
  `AspireUseCliBundle` stays `false`; that keeps the dashboard inside NuGet audit, which is how its
  critical advisory surfaced. `ASPIRE010` (CLI bundle off) is suppressed in the AppHost for this
  reason; switching to the CLI bundle would take the dashboard out of NuGet audit.
- **Shared libraries take ASP.NET Core via `<FrameworkReference Include="Microsoft.AspNetCore.App" />`**
  (`Mango.Infrastructure`). The legacy `Microsoft.AspNetCore.Http*` 2.3.x NuGet packages are a
  trap: even the newest 2.3.14 pulls `Microsoft.AspNetCore.WebUtilities` 2.3.13, below the
  CVE-2026-118750 fix.
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
`.csproj` files. `Directory.Build.props` sets `WarningsAsErrors` for **CS1998 and CS4014 only** —
other warnings do not fail the build, and the solution carries ~100 pre-existing nullable warnings
(mostly `Identity.API`, `Mango.Web`). Judge "no new warnings" against a baseline, not zero.
