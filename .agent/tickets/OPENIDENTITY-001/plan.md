# Technical Blueprint: OPENIDENTITY-001 (OpenIdentity App)

### 📝 Business Summary
We need to introduce a new authentication microservice called `OpenIdentity` using **OpenIddict**. This new service will run parallel to the existing `Identity.API` app (without removing it). The `OpenIdentity` service will require its own separate database to store users, roles, and OpenIddict's tokens/authorizations.

### 🏗️ Technical Impact Map
- **Backend:**
  - Create a new project `src/Services/OpenIdentity` (ASP.NET Core Web API or MVC depending on UI needs).
  - Add EF Core, ASP.NET Core Identity, and OpenIddict packages.
  - Entities: `ApplicationUser`, along with OpenIddict's default entities (`OpenIddictEntityFrameworkCoreApplication`, etc.).
  - Database: A new SQL Server database (e.g., `MangoAuth_OpenIddict`).
  - Configuration: `appsettings.json` needs its own connection string and OpenIddict certificate/keys setup.
  - **Refactor:** Move Management APIs (Clients, Resources, Roles) to Minimal APIs in `Program.cs`.
  - **Refactor:** Align `Program.cs` with the project pattern by moving code to extension methods.
- **Frontend / UI Layer:**
  - If it involves a UI for Login/Register (Authorization Server), we will implement basic Razor Views (similar to how `Identity.API` does it) or integrate a React app if planned. For now, assuming standard ASP.NET Identity UI pages with OpenIddict endpoints (`/connect/token`, `/connect/authorize`).

### 🧪 Acceptance Criteria (AC)
- Given a user needs to authenticate, when they hit `/connect/token` on `OpenIdentity`, then a valid OpenId Connect JWT token is issued.
- Given a user is registering, when they submit their details, then they are securely saved in the new `OpenIdentity` database.
- Given the system runs, when both `Identity.API` and `OpenIdentity` start, then they do not conflict (different ports, different databases).
- Management APIs are accessible via Minimal API endpoints with Admin role protection.
- Secrets are not hardcoded in `DbInitializer`.

### ❓ Clarifying Questions (Resolved)
1. **UI:** `OpenIdentity` will serve Razor Pages for Login/Register, similar to the current custom UI of `Identity.API`.
2. **Features:** OpenIddict endpoints will include Authentication (`/connect/token`, `/connect/authorize`), getting user data (`/connect/userinfo`), managing roles, managing clients, and managing resources (API controllers for CRUD).
3. **Orchestration:** `OpenIdentity` will be directly registered into the .NET Aspire `Mango.AppHost`.
4. **Refactor:** Management APIs shifted to Minimal APIs.
5. **Fixes:** Applying all 10 fixes from the code review (Security, Architecture, Quality).
