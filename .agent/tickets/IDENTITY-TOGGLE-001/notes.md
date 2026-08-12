# IDENTITY-TOGGLE-001 — Notes

> State lives in `ticket.json`. Migrated from the retired `docs/tracking/IDENTITY-TOGGLE-001.md`.
> **This ticket is still open** — runtime verification of both providers has not been exercised
> end-to-end.

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

- Nothing blocking. The remaining work is execution, not a decision.

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

### 2026-08-12 (harness migration)

Ticket state moved from `docs/tracking/IDENTITY-TOGGLE-001.md` into this directory. No progress
changed in the move: steps 1–4 remain done, step 5 remains open with tasks 5.1 and 5.2 pending.
