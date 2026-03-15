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
