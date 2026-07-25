# Tech Stack

## Backend
- **.NET 10** (`net10.0`), **C# 14** (extension blocks for route mapping, primary constructors, file-scoped namespaces).
- Nullable reference types **enabled**; implicit usings enabled.
- **.NET Aspire** for orchestration/service discovery (`Mango.AppHost`, `Mango.ServiceDefaults`).
- **Minimal APIs** (no MVC controllers) + **Mediator** (custom `src/Shared/Mediator`, MediatR-style) for request handling.
- **EF Core** with **Npgsql** (PostgreSQL). Aspire integrations: `AddNpgsqlDataSource`, `EnrichNpgsqlDbContext<T>`.
- **FluentValidation** for command/query validation.
- Messaging: **RabbitMQ** via `EventBus.RabbitMQ` (abstraction `IEventBus`; Azure Service Bus alt in `EventBus.ServiceBus`). **Debezium** for CDC.
- Identity: **Duende IdentityServer** (`Identity.API`) and **OpenIddict** (`OpenIdentity.App`, + `OpenIddict.Quartz`, `Quartz`).

## Frontend (`src/UI/mango-ui`)
- **React 19 + Vite + TypeScript** (strict mode; no `any`).
- **pnpm** package manager (`pnpm-lock.yaml`) — NOT npm.
- ESLint (`eslint.config.js`). No test runner configured.
- Path alias `@/` (Vite + tsconfig).

## Dependency management
- **Central Package Management** via `Directory.Packages.props` — do not pin versions in individual `.csproj` files.
