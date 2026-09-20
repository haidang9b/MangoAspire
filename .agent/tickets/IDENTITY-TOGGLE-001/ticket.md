---
id: IDENTITY-TOGGLE-001
kind: feature
status: in_progress
priority: high
createdUtc: 2026-03-12T00:00:00Z
updatedUtc: 2026-08-12T00:00:00Z
completedUtc: null
tags: [backend, identity, apphost, gateway]
---

# IDENTITY-TOGGLE-001 — Toggle the identity provider between Duende (Identity.API) and OpenIddict (OpenIdentity.App)

> **Still open** — runtime verification of both providers has not been exercised end-to-end.

## Plan

### Requirement
Let the platform boot with either Duende (`Identity.API`) or OpenIddict (`OpenIdentity.App`) as the
active identity provider, selected by configuration, with no provider-specific branching left in
any consuming microservice.

### Approach
- `IdentityType` (`Duende` | `OpenIddict`) is read from `appsettings.json`, the `IdentityType` /
  `IDENTITY_TYPE` environment variables, or `--IdentityType`. Unrecognised values **throw at
  startup** rather than falling back to a default.
- Only the selected provider is registered; every consumer binds to a single `identityRef`.
- The authority is propagated as `ServiceUrls__IdentityApp` from the resolved `identityEndpoint`.
  JWT `Authority` binds from `ServiceUrls:IdentityApp` — no provider-specific branching remains in
  any microservice.
- `Mango.Core.Options.SeedUsersOptions` gives both providers one seed-account contract with fixed
  IDs, keeping the OIDC `sub` claim stable across a switch.
- The gateway needs no change: it proxies the business APIs, and both frontends reach the identity
  provider directly.

### Risks and open questions
Nothing blocking — the remaining work is execution (end-to-end verification), not a decision.

## Steps
- [x] **Step 1 — Analysis and planning**
  - [x] Review the analyze-requirement workflow and analyze-po-requirement skill
  - [x] Analyse the impact on Mango.AppHost (orchestration)
  - [x] Analyse the impact on the microservices (authentication middleware)
  - [x] Analyse the impact on the frontends (Mango.Web and mango-ui)
  - [x] Write the technical blueprint and clarifying questions
  - [x] Blueprint approved by the user
- [x] **Step 2 — Implementation: core infrastructure**
  - [x] Conditional service registration in Mango.AppHost behind the IdentityType flag
  - [x] Shared authentication configuration that respects the flag
- [x] **Step 3 — Implementation: microservices**
  - [x] Update Products.API, ShoppingCart.API, Orders.API and the rest
  - [x] WebApplicationBuilderExtensions handles both OIDC providers
- [x] **Step 4 — Implementation: frontend and gateway**
  - [x] ~~Update Mango.Gateway (YARP) routing~~ — skipped: no change required, the gateway proxies
        the business APIs and both frontends reach the identity provider directly
  - [x] Point Mango.Web and mango-ui at the resolved authority
- [ ] **Step 5 — Verification**
  - [ ] Verify system functionality end-to-end with Identity.API (Duende) enabled
  - [ ] Verify system functionality end-to-end with OpenIdentity.App (OpenIddict) enabled
  - [x] Final documentation update and walkthrough

## Links
- Branch:
- Commits:
- PRs:
- Files: src/Mango.AppHost/Program.cs, src/Shared/Mango.Core/Options/SeedUsersOptions.cs
- Docs: docs/architecture/overview.md, docs/api/endpoints.md, docs/ARCHITECTURE.md
- Memory: .agent/memory/domains/identity.md

## Decisions
- **`IdentityType` (`Duende` | `OpenIddict`)** is read from `appsettings.json`, the `IdentityType`
  or `IDENTITY_TYPE` environment variables, or `--IdentityType`. Unrecognised values **throw at
  startup** rather than falling back to a default, so a typo cannot silently boot the wrong
  provider.
- Only the selected provider is registered; every consumer binds to a single `identityRef`.
- The authority is propagated as `ServiceUrls__IdentityApp` from the resolved `identityEndpoint`
  instead of being configured per service. JWT `Authority` binds from `ServiceUrls:IdentityApp`, so
  **no provider-specific branching remains in any microservice**.
- `Mango.Core.Options.SeedUsersOptions` gives both providers one seed-account contract with **fixed
  IDs**, which keeps the OIDC `sub` claim stable across a switch.
- The gateway needed no change: it proxies the business APIs, and both frontends reach the identity
  provider directly.

## Gotchas
- **Scope claims have different shapes per provider.** Duende emits one claim per scope; OpenIddict
  emits a single space-delimited claim. `RequireClaim("scope", "mango")` therefore passes under one
  and fails under the other. The `ApiScope` policies now use a **whitespace-splitting assertion**.
  Do not revert this.
- **Client configuration shapes differ**: `OpenIddict__Clients__*` versus
  `IdentityServer__Clients__*`. The SPA client is seeded only by `OpenIdentity.App`; Duende declares
  its clients statically in `Identity.API` appsettings.
- Neither frontend required a source change — `Mango.Web` reads `ServiceUrls__IdentityApp` and
  `OpenIdConnect__Authority`, `mango-ui` reads `VITE_IDENTITY_URL`, and both values come from the
  resolved endpoint.

All of the above is recorded in `.agent/memory/domains/identity.md`.

## Open Questions
Nothing blocking. The remaining work is execution, not a decision.

## Blockers
None open.

## Session Log

### 2026-03-12
Analysis, blueprint and approval completed. Core infrastructure, microservice and frontend/gateway
implementation all landed.

### 2026-08-12
Documentation updated across `docs/architecture/overview.md` (Identity Provider Switch),
`docs/api/endpoints.md` (provider-agnostic scope policies) and `docs/ARCHITECTURE.md`.

**Still outstanding:** boot the system once with `IdentityType=Duende` and once with
`IdentityType=OpenIddict`, and exercise login → cart → checkout under each. Until both runs pass,
the toggle is implemented but unproven.

### 2026-09-20 (harness migration)
Ticket state moved from the JSON `ticket.json` + `notes.md` + `plan.md` trio into this single
`ticket.md`. No progress changed in the move: steps 1–4 remain done, step 5 remains open with tasks
5.1 and 5.2 pending.
