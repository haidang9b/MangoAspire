# Project Core — MangoAspire

.NET 10 + .NET Aspire microservices e-commerce platform with a React/Vite SPA.
Dual identity providers (Duende IdentityServer via `Identity.API` and OpenIddict via `OpenIdentity.App`), PostgreSQL, RabbitMQ, Debezium.

## Source map (`src/`)
- `Mango.AppHost/` — Aspire AppHost; orchestrates all services locally (run entrypoint).
- `Mango.ServiceDefaults/` — shared observability/resiliency defaults referenced by every service.
- `Gateway/` — API gateway.
- `Services/` — the microservices (Vertical Slice Architecture, see `mem:conventions`):
  - `Products.API`, `Orders.API`, `Payments.API`, `ShoppingCart.API`, `Coupons.API` (note: legacy `Coupon.API` also present)
  - `Identity.API` (Duende), `OpenIdentity.App` (OpenIddict) — dual identity, toggleable
  - `Mango.Orchestrators` — **saga orchestrator** service (see `mem:checkout_saga`)
  - `ChatAgent.App`
- `Shared/` — `Mango.Core`, `Mango.Common`, `Mango.Events`, `Mango.Infrastructure`, `Mango.RestApis`, `Mediator`, `Mango.ServiceDefaults`.
- `EventBus`, `EventBus.RabbitMQ`, `EventBus.ServiceBus` — abstracted event bus (`IEventBus`), RabbitMQ default.
- `UI/mango-ui/` — React 19 + Vite + TypeScript SPA (see `mem:frontend/core`).

## Tests (`tests/Services/`)
`Coupons.API.Tests`, `Orders.API.Tests`, `Payments.API.Tests`, `Products.API.Tests`, `ShoppingCart.API.Tests`. xUnit + Moq + Shouldly.

## Project-wide invariants
- Solution file: `MangoAspire.sln` at repo root.
- Authoritative agent guidance lives in `AGENTS.md` + `.agent/` (rules, skills, workflows); `CLAUDE.md` is a thin shim importing them.
- Claude Code native harness (`.claude/skills|agents|commands`) is **generated** from `.agent/` via `pwsh ./scripts/sync-agent-harness.ps1`. Do not hand-edit generated files.
- Serena MCP (this server) provides semantic C# navigation, wired via `.mcp.json` → `http://localhost:9121/mcp`. See `mem:suggested_commands`.
