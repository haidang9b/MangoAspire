---
id: SEC-AUTH-001
kind: security
status: completed
priority: high
createdUtc: 2026-11-13T09:27:04Z
updatedUtc: 2026-11-13T11:45:00Z
completedUtc: 2026-11-13T11:45:00Z
tags: [zero-trust, auth, jwt, signing-key, products-api, coupons-api]
---

<!--
kind: feature | bug | chore | security | refactor | spike
status: not_started | in_progress | awaiting_approval | blocked | completed | abandoned
State lives in this file only. A step is done only when every task under it is checked or
struck through as skipped. Do not maintain progress anywhere else.
-->

# SEC-AUTH-001 — Rotate the committed Duende signing key and require auth on Products and Coupons

## Plan
*Approved by the user before Step 2 starts.*

### Threat / goal

A zero-trust review (2026-11-13) found that service-to-service calls are only as safe as the
receiving service, and two receivers check nothing. This ticket fixes the two most urgent findings:

1. **Critical: the Duende private signing key is in git.** `src/Services/Identity.API/tempkey.jwk`
   holds a full RSA private key (`D`, `P`, `Q`), and `AddDeveloperSigningCredential()`
   (`Identity.API/Extensions/IdentityServerExtensions.cs:49`) loads that exact file. Anyone with
   the repo can mint a token for any user with `scope: mango`, and ShoppingCart, Orders and
   ChatAgent accept it (they check signature, issuer and expiry only — `ValidateAudience = false`).
2. **High: Products.API and Coupons.API have no authentication.** Anyone can create, update or
   delete products and trigger a CDC snapshot (`POST /api/products/cdc-snapshots`). The gateway
   route `/api/products/{**catch-all}` exposes all of it. Coupons can be enumerated anonymously.
   ShoppingCart and ChatAgent forward the user's token to Coupons, but Coupons ignores it.

Found while planning:

3. **High: anyone can register as Admin.** Both registration pages offer an "Admin" role in the
   form. Duende `AccountController.Register` assigns whatever `RoleName` is posted (and creates the
   role if it does not exist); OpenIddict `AccountController.Register` assigns any role that exists,
   including Admin. Until this is fixed, an Admin check on Products writes is not a real control.

### Scope

In:
- Remove `tempkey.jwk` and `keys/is-signing-key-*.json` from git, ignore them, delete the local
  copies so fresh keys are generated on next start (rotation).
- One shared `AddMangoJwtAuthentication()` extension in `Mango.Infrastructure` that registers
  JwtBearer (authority from `ServiceUrls:IdentityApp`) plus the `ApiScope` policy (whitespace-split
  scope check, unchanged) and a new `Admin` policy (`ApiScope` + role `Admin`).
- Products.API: `GET` products / product by id / catalog-types stay **anonymous** (catalog
  browsing). `POST`/`PUT`/`DELETE /api/products` and `POST /api/products/cdc-snapshots` require
  the `Admin` policy.
- Coupons.API: `GET /api/coupons/{code}` requires `ApiScope`.
- AppHost: give `products-api` and `coupons-api` the identity reference and `ServiceUrls__IdentityApp`.
- Pending user decision (see Open Questions): fix Admin self-registration; migrate ShoppingCart,
  Orders, ChatAgent onto the shared extension.

Out (follow-up tickets):
- Audience validation per API (`ValidateAudience = false` everywhere).
- Service identity (client-credentials / token exchange) for service-to-service calls.
- Per-service RabbitMQ users, message trust in the saga, TLS on AMQP.
- Orders using the default policy instead of `ApiScope`.
- Rewriting git history. Not needed for safety: once rotated, the old key is no longer published
  in the IdP's JWKS, so tokens signed with it are rejected.

### Steps

1. Branch `fix/SEC-AUTH-001-service-auth` from `master` (current branch is already merged).
2. Key rotation — `git rm --cached` both key files, delete local copies, add `**/tempkey.jwk` and
   `src/Services/Identity.API/keys/` to `.gitignore`.
3. Shared auth extension — `src/Shared/Mango.Infrastructure/Extensions/` + JwtBearer package
   reference in `Mango.Infrastructure.csproj` (version already central in
   `Directory.Packages.props`).
4. Products.API — `IServiceCollectionExtensions`, `WebApplicationExtensions`
   (`UseAuthentication`/`UseAuthorization`), `Routes/ProductEndpoints.cs`, `Routes/CdcEndpoints.cs`
   (drop the "unauthenticated" NOTE).
5. Coupons.API — `IServiceCollectionExtensions`, `WebApplicationExtensions`,
   `Routes/CouponEndponts.cs`.
6. AppHost — `src/Mango.AppHost/AppHost.cs` products + coupon resources.
7. (If approved) Registration — both IdPs always assign `Customer`; remove the role picker from
   the register views. Admins come only from `SeedUsers`.
8. (If approved) Move ShoppingCart, Orders, ChatAgent onto `AddMangoJwtAuthentication()`.

### Verification

- `dotnet build MangoAspire.sln` with zero warnings (`WarningsAsErrors` is on).
- `dotnet test MangoAspire.sln`.
- Unit tests for the shared policies: `ApiScope` accepts both scope shapes (Duende multi-claim,
  OpenIddict space-delimited); `Admin` rejects a non-admin and a token without `mango`.
- `git ls-files | grep -E "tempkey.jwk|is-signing-key"` returns nothing.
- If Docker is available: run the AppHost and confirm anonymous `POST /api/products` → 401,
  customer token → 403, anonymous `GET /api/products` → 200, anonymous `GET /api/coupons/X` → 401.

### Risks and open questions

- Rotating the key signs everyone out under Duende; existing tokens fail. Expected.
- Other developers' clones: pulling the `git rm` deletes their local `tempkey.jwk`, so their IdP
  rotates too.
- Products and Coupons will now call the IdP for metadata at first request; if the IdP is down,
  writes fail closed (correct behaviour).

## Steps
- [x] **Step 1 — Scope and plan**
  - [x] Define the vulnerability/hardening goal and scope
  - [x] Write the plan above
  - [x] Plan approved by the user
- [x] **Step 2 — Execute**
  - [x] Branch from `master`
  - [x] Rotate and untrack the Duende signing keys
  - [x] Shared `AddMangoJwtAuthentication()` extension
  - [x] Products.API auth (anonymous reads, Admin writes + CDC)
  - [x] Coupons.API auth
  - [x] AppHost wiring for products-api and coupons-api
  - [x] Registration can no longer grant Admin
  - [x] Migrate ShoppingCart, Orders, ChatAgent to the shared extension
- [x] **Step 3 — Verification**
  - [x] Affected build/lint/tests passing — build 0 errors; `dotnet test` 272/272; `dotnet format --verify-no-changes` clean on changed files
  - [x] Security check clean (scan/audit shows no new findings) — no key files tracked or in `bin/`; both Coupons callers forward the user token; no other role-assignment path
  - [x] Zero compiler warnings (fix-warnings), if code changed — none introduced; 10 pre-existing warnings in untouched files left alone
- [x] **Step 4 — Documentation, memory and completion**
  - [x] Sync `/docs`, if applicable
  - [x] Conventional commit created — d0f483e, 80b1cfb, 9e4dcf8, cea89bc, 46d1d1d, plus this ticket commit
  - [x] Promote Decisions/Gotchas below into `.agent/memory/domains/`, if non-obvious

## Links
- Branch: `fix/SEC-AUTH-001-service-auth` (from `master` @ 15e0fdf)
- Commits: d0f483e (key rotation), 80b1cfb (registration), 9e4dcf8 (shared auth extension),
  cea89bc (Products/Coupons auth), 46d1d1d (docs + memory)
- PRs:
- Files: `src/Services/Identity.API/tempkey.jwk`, `src/Services/Identity.API/keys/`,
  `src/Services/Identity.API/Extensions/IdentityServerExtensions.cs`, `.gitignore`,
  `src/Shared/Mango.Infrastructure/`, `src/Services/Products.API/{Extensions,Routes}/`,
  `src/Services/Coupons.API/{Extensions,Routes}/`, `src/Mango.AppHost/AppHost.cs`,
  `src/Services/Identity.API/MainModule/Account/AccountController.cs`,
  `src/Services/OpenIdentity.App/Controllers/AccountController.cs`
- Docs: `docs/api/endpoints.md` (Security Overview), `docs/CDC.md` (snapshot endpoint auth)
- Memory: `.agent/memory/domains/identity.md` (signing keys, roles, shared extension)

## Decisions
- Product and catalog reads stay anonymous: `Mango.Web` `HomeController` and the SPA home page
  browse the catalog before login.
- Coupons require a token: every caller (ShoppingCart `ApplyCoupon`/`CheckoutHandler`, ChatAgent
  `CouponsPlugin`, Mango.Web `CartController`, SPA cart) runs inside an authenticated request and
  already forwards the user's token.
- Role name is `Admin` (exact case) in both IdPs' `SeedUsers`; `RequireRole` compares ordinally.
  Duende `ProfileService` and OpenIddict `GetDestinations` both put `role` in the access token.

- User approved (2026-11-13): fix Admin self-registration in this ticket — new users always get
  `Customer`; Admin accounts come only from `SeedUsers`.
- User approved (2026-11-13): move ShoppingCart, Orders, ChatAgent onto the shared
  `AddMangoJwtAuthentication()` in this ticket, behaviour unchanged.

## Gotchas
- The Web SDK treats every `*.json` as `Content`, so Duende's `keys/is-signing-key-*.json` was copied
  into `bin/` and would ship in publish output and container images. `Identity.API.csproj` now removes
  `keys\**` and `tempkey.jwk` from Content/None.
- The plan assumed `WarningsAsErrors` makes the build warning-free. It does not cover nullable
  warnings: the build has 10 pre-existing warnings (CS8601, CS8602, CS8981) in files this ticket does
  not touch.
- Orders.API routes use `.RequireAuthorization()` (default policy), not `ApiScope`, so Orders does not
  check the `mango` scope. Out of scope here; recorded in `docs/api/endpoints.md`.
- Duende `Register` still adds a hardcoded `role: User` claim. It is inert (not `Admin`) and was left
  as-is.
- Accounts that self-registered as Admin before this fix still hold the role. Remediation is a data
  operation (audit the `Admin` role membership in each provider's database) and is not done by this
  ticket.
## Open Questions

## Blockers
*One `### blk-N — <one-line summary> (severity: low|medium|high, opened: YYYY-MM-DD, resolved: YYYY-MM-DD|open)` heading per blocker.*

## Session Log
*Append-only, newest last. One `### YYYY-MM-DD` heading per working session.*

### 2026-11-13
- Zero-trust review done; ticket opened for findings #1 (committed signing key) and #2 (Products /
  Coupons unauthenticated). Found Admin self-registration while planning. Plan awaiting approval.
- Resumed: every Step 2 task was already implemented in the working tree. Verified that Mango.Web
  (`AddAuthToken()`) and the SPA (axios interceptor) send the user's token, so Admin-only product
  writes keep working from both admin UIs.
- Verification: build 0 errors, 272/272 tests (17 new), format clean. Docker not running, so the live
  AppHost check (401/403/200) was skipped.
- Docs and memory synced. User approved the commit; landed as five conventional commits plus the
  ticket commit. Open follow-ups: live AppHost check (401/403/200), audit of self-registered Admin
  accounts, and the out-of-scope items in the Plan.
