# Architecture — source map and invariants

.NET 11 + .NET Aspire microservices e-commerce platform with a React/Vite SPA.
Dual identity providers (Duende IdentityServer via `Identity.API`, OpenIddict via
`OpenIdentity.App`), PostgreSQL, RabbitMQ, Debezium.

## Source map (`src/`)

- `Mango.AppHost/` — Aspire AppHost; orchestrates all services locally (the run entrypoint).
- `Mango.ServiceDefaults/` — shared observability/resiliency defaults referenced by every service.
- `Gateway/` — API gateway (YARP). Proxies the business APIs only; both frontends reach the identity
  provider directly.
- `Services/` — the microservices, Vertical Slice Architecture:
  - `Products.API`, `Orders.API`, `Payments.API`, `ShoppingCart.API`, `Coupons.API`
  - `Identity.API` (Duende), `OpenIdentity.App` (OpenIddict) — mutually exclusive; the AppHost
    starts exactly one. See `.agent/memory/domains/identity.md`.
  - `Mango.Orchestrators` — the saga orchestrator service. See
    `.agent/memory/domains/checkout-saga.md`.
  - `ChatAgent.App` — RAG chat agent with guardrails (`docs/CHAT_AGENT_RAG.md`).
- `Shared/` — `Mango.Core`, `Mango.Events`, `Mango.Infrastructure`, `Mango.RestApis`, `Mediator`,
  `Mango.ServiceDefaults`.
  - Caching: `ICacheManager` (`Mango.Core.Caching`) → `HybridCacheManager`
    (`Mango.Infrastructure.Caching`, HybridCache), registered via `AddCacheManager()`.
- `EventBus`, `EventBus.RabbitMQ`, `EventBus.ServiceBus` — abstracted event bus (`IEventBus`),
  RabbitMQ by default.
- `UI/mango-ui/` — React 19 + Vite + TypeScript SPA. See
  `.agent/memory/domains/frontend-spa.md`.

## Tests (`tests/Services/`)

`ChatAgent.App.Tests`, `Coupons.API.Tests`, `Orders.API.Tests`, `Payments.API.Tests`,
`Products.API.Tests`, `ShoppingCart.API.Tests`. xUnit + Moq + Shouldly.

## Project-wide invariants

- Solution file: `MangoAspire.sln` at the repo root.
- Agent guidance:
  - `AGENTS.md` is the always-on index, and `CLAUDE.md` imports it.
  - Rules, skills and agents live in `.claude/`.
  - `.github/instructions/` and `.github/agents/` are a generated Copilot mirror.
  - See `.agent/tools/harness.md`.

## Deliberate exceptions to the stated rules

These look like violations and are not — do not "fix" them:

- **`Mango.Orchestrators` uses `Repositories/SagaRepository.cs`** even though the rest of the
  codebase forbids the repository pattern. Saga state persistence is the one place it is wanted.
- `Mango.Common` was removed in commit `2a525cf`; references to it in older docs are stale.
