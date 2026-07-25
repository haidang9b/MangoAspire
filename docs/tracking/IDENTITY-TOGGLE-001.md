# Ticket: IDENTITY-TOGGLE-001 Tracker

## Step 1: Analysis and Planning
- [x] Review `analyze-requirement` workflow and `analyze-po-requirement` skill
- [x] Analyze impact on `Mango.AppHost` (Orchestration)
- [x] Analyze impact on individual microservices (Authentication Middleware)
- [x] Analyze impact on Frontend applications (`Mango.Web` and `mango-ui`)
- [x] Create Technical Blueprint and Clarifying Questions
- [x] Obtain user approval for the plan

## Step 2: Implementation (Core Infrastructure)
- [x] Update `Mango.AppHost` to support conditional service registration based on a feature flag
  - `IdentityType` (`Duende` | `OpenIddict`) read from `appsettings.json`, `IdentityType` / `IDENTITY_TYPE` env vars, or `--IdentityType`; unrecognised values throw at startup.
  - Only the selected provider is registered; all consumers bind to a single `identityRef`.
- [x] Create a shared configuration/extension for authentication that respects the flag
  - Authority is propagated as `ServiceUrls__IdentityApp` from the resolved `identityEndpoint` rather than being configured per service.
  - `Mango.Core.Options.SeedUsersOptions` gives both providers one seed-account contract with fixed IDs, keeping the OIDC `sub` claim stable across a switch.

## Step 3: Implementation (Microservices)
- [x] Update `Products.API`, `ShoppingCart.API`, `Orders.API`, etc.
  - `ApiScope` policies replaced `RequireClaim("scope", "mango")` with a whitespace-splitting assertion, so both Duende (one claim per scope) and OpenIddict (space-delimited) tokens satisfy them.
- [x] Ensure `WebApplicationBuilderExtensions` can handle both OIDC providers
  - JWT `Authority` is bound from `ServiceUrls:IdentityApp`; no provider-specific branching remains in the services.

## Step 4: Implementation (Frontend & Gateway)
- [x] Update `Mango.Gateway` (YARP) routing if necessary — no change required; the gateway proxies the business APIs, and both frontends reach the identity provider directly.
- [x] Update `Mango.Web` and `mango-ui` to point to the correct authority
  - `Mango.Web` receives `ServiceUrls__IdentityApp` and `OpenIdConnect__Authority`; `mango-ui` receives `VITE_IDENTITY_URL`. Both come from the resolved endpoint, so no source change was needed.
  - Client redirect / post-logout URIs are pushed into whichever provider is active, using that provider's configuration shape (`OpenIddict__Clients__*` vs `IdentityServer__Clients__*`). The SPA client is only seeded by `OpenIdentity.App`; Duende declares its clients statically in `Identity.API` appsettings.

## Step 5: Verification
- [ ] Verify system functionality with `Identity.API` enabled
- [ ] Verify system functionality with `OpenIdentity.App` enabled
- [x] Final documentation update and walkthrough
  - Covered in `docs/architecture/overview.md` (Identity Provider Switch), `docs/api/endpoints.md` (provider-agnostic scope policies), and `docs/ARCHITECTURE.md`.

> Runtime verification of both providers is still outstanding — the two boxes above have not been exercised end-to-end.
