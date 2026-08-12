# Identity — the Duende / OpenIddict provider switch

Two identity providers exist and are **mutually exclusive**: `Identity.API` (Duende IdentityServer)
and `OpenIdentity.App` (OpenIddict). The AppHost starts exactly one.

## The switch

- `IdentityType` (`Duende` | `OpenIddict`) is read from `appsettings.json`, the `IdentityType` /
  `IDENTITY_TYPE` environment variables, or `--IdentityType`. **Unrecognised values throw at
  startup** — deliberately, so a typo cannot silently boot the wrong provider.
- Only the selected provider is registered; every consumer binds to a single `identityRef`.
- The authority is propagated as `ServiceUrls__IdentityApp` from the resolved `identityEndpoint`
  rather than being configured per service. JWT `Authority` binds from `ServiceUrls:IdentityApp`, so
  **no provider-specific branching remains in the microservices**.

## Seed accounts

`Mango.Core.Options.SeedUsersOptions` gives both providers one seed-account contract with **fixed
IDs**, which keeps the OIDC `sub` claim stable across a provider switch. Changing those IDs
invalidates existing tokens and any data keyed by `sub`.

## Scope policy — the whitespace trap

Duende emits one claim per scope; OpenIddict emits a single space-delimited claim. A
`RequireClaim("scope", "mango")` policy therefore passes under one provider and fails under the
other. `ApiScope` policies use a **whitespace-splitting assertion** instead, so both token shapes
satisfy them. Do not revert this to `RequireClaim`.

## Client configuration shapes differ

Redirect and post-logout URIs are pushed into whichever provider is active, using that provider's
own configuration shape: `OpenIddict__Clients__*` versus `IdentityServer__Clients__*`.

- The SPA client is seeded **only** by `OpenIdentity.App`.
- Duende declares its clients **statically** in `Identity.API` appsettings.

## Frontends

Neither frontend needed a source change for the switch — both read the resolved endpoint from
configuration:

- `Mango.Web` receives `ServiceUrls__IdentityApp` and `OpenIdConnect__Authority`.
- `mango-ui` receives `VITE_IDENTITY_URL`.

The gateway (YARP) required no change: it proxies the business APIs, and both frontends reach the
identity provider directly.
