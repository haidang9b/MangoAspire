# Technical Blueprint: IDENTITY-TOGGLE-001 (Identity Provider Switch)

### 📝 Business Summary
We need a way to toggle between the existing `Identity.API` (Duende) and the new `OpenIdentity.App` (OpenIddict) as the centralized authentication provider for the entire Mango e-commerce ecosystem. This toggle should be manageable at the orchestration level (`AppHost`) and automatically propagate to all dependent microservices (`ShoppingCart`, `Orders`, `ChatAgent`) and frontend apps (`Mango.Web`).

### 🏗️ Technical Impact Map
- **Orchestration (`Mango.AppHost`):**
  - Introduce an `IdentityType` parameter or environment variable.
  - Dynamically assign the `identityEndpoint` based on the selected provider.
  - Update `WithReference` and `WithEnvironment("ServiceUrls__IdentityApp", ...)` to point to the active service.
- **Identity Services:**
  - **`OpenIdentity.App`:** Sync client IDs, secrets, and scopes (use `mango` scope instead of `api`) to ensure compatibility with existing microservice policies.
- **Consumer Microservices:**
  - `ShoppingCart.API`, `Orders.API`, and `ChatAgent.App` will stay the same as they already consume the authority from environment variables.
- **Frontend (`Mango.Web`):**
  - Will receive the new `OpenIdConnect__Authority` from `AppHost`.

### 🧪 Acceptance Criteria (AC)
- Given `IdentityType` is set to `Duende`, when the system starts, then `Identity.API` is the authority and all services validate tokens against it.
- Given `IdentityType` is set to `OpenIddict`, when the system starts, then `OpenIdentity.App` is the authority and all services validate tokens against it.
- Given a switch occurs, then no manual code changes are required in the microservice logic or authentication middleware.

### ❓ Clarifying Questions
1. **Default Provider:** Should `Identity.API` (Duende) remain the default for now?
2. **User Data Persistence:** Do we need to migrate users between `Identity.API` and `OpenIdentity.App` databases, or is it okay if they use separate databases for testing?
3. **Environment:** Is this toggle intended only for `Development` locally via `AppHost`, or should it be deployable to production?
4. **Client Secrets:** Should we use identical client secrets for both providers in the `DbInitializer` for seamless switching?

---

## Current State (Observed)
- `Mango.AppHost` always resolves `identityEndpoint` from `Identity.API` and injects it into:
  - `ServiceUrls__IdentityApp` for `ShoppingCart.API`, `Orders.API`, `ChatAgent.App`, `Mango.Web`.
  - `OpenIdConnect__Authority` for `Mango.Web`.
- `OpenIdentity.App` seeds scope `api` (not `mango`) and creates `mango-services` + `mango-web` clients.
- `ServiceUrlsOptions.IdentityApp` default is `https://identity-app`.

---

## Proposed Approach
- Add a single toggle at `Mango.AppHost` level, e.g. `IdentityType` parameter or env var.
- When toggle = `Duende`, route all identity-dependent endpoints to `Identity.API`.
- When toggle = `OpenIddict`, route all identity-dependent endpoints to `OpenIdentity.App`.
- Keep microservices unchanged; only the authority/identity endpoint shifts via environment variables.

---

## Implementation Steps
1. **Add toggle input**
   - Add `IdentityType` to AppHost via `builder.AddParameter(...)` or environment variable.
   - Define allowed values: `Duende` | `OpenIddict`. Default to `Duende` unless clarified.
2. **Resolve active identity**
   - Compute `identityEndpoint` from the selected project (`identity` or `openIdentity`).
   - Set a single `identityRef` variable for `.WithReference(...)`.
3. **Wire references & env**
   - Use `identityRef` in `WithReference(...)` for downstream services.
   - Inject `ServiceUrls__IdentityApp` and `OpenIdConnect__Authority` with `identityEndpoint`.
4. **OpenIdentity compatibility**
   - Update `OpenIdentity.App` seed data:
     - Scope should be `mango` (replace `api`) if downstream policies expect `mango`.
     - Ensure client IDs/secrets align with `Identity.API` equivalents.
5. **Redirect URIs**
   - When `OpenIddict` active, configure redirect/post-logout URIs for `Mango.Web` in OpenIdentity if needed.

---

## Config Matrix (AppHost)
| IdentityType | Authority (identityEndpoint) | WithReference Target | Authority Consumers |
|---|---|---|---|
| Duende | `Identity.API` endpoint | `Identity.API` project | `ShoppingCart.API`, `Orders.API`, `ChatAgent.App`, `Mango.Web` |
| OpenIddict | `OpenIdentity.App` endpoint | `OpenIdentity.App` project | `ShoppingCart.API`, `Orders.API`, `ChatAgent.App`, `Mango.Web` |

---

## Verification Plan
1. Start with `IdentityType=Duende` and verify login + JWT validation across services.
2. Start with `IdentityType=OpenIddict` and verify login + JWT validation across services.
3. Confirm `Mango.Web` sign-in and sign-out redirects work for both providers.
4. Confirm `mango-services` client credentials flow works against selected provider.

---

## Rollback Plan
- Set `IdentityType=Duende` and restart AppHost.
- No code changes required in microservices.

---

## Open Questions (Carry Forward)
1. Default provider and scope naming (`mango` vs `api`).
2. Whether production deployments require the toggle or only local/dev.
3. Whether client secrets must be identical across providers.
