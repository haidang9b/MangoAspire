---
id: OPENIDENTITY-001
kind: feature
status: completed
priority: normal
createdUtc: 2026-03-11T00:00:00Z
updatedUtc: 2026-03-11T00:00:00Z
completedUtc: 2026-03-11T00:00:00Z
tags: [backend, identity, apphost]
---

# OPENIDENTITY-001 — Add OpenIdentity.App — an OpenIddict authorization server alongside Identity.API

## Plan

### Requirement
Add `OpenIdentity.App`, an OpenIddict-based authorization server, running alongside the existing
Duende-based `Identity.API` (not replacing it).

### Approach
- `OpenIdentity.App` runs **in parallel with** `Identity.API` — different ports, different
  databases, no conflict at startup. Choosing between them at runtime came later, in
  `IDENTITY-TOGGLE-001`.
- It serves its own Razor login/register pages, mirroring `Identity.API`'s custom UI, rather than
  scaffolding the default ASP.NET Identity UI.
- Management APIs (clients, resources, roles) were written as **Minimal APIs** with admin-role
  protection, matching the repo's Minimal-API-only convention.
- The service is registered directly in the Aspire AppHost.

### Touchpoints
New `OpenIdentity.App` project (EF Core, ASP.NET Identity, OpenIddict), own database, Razor UI,
`/connect/*` endpoints, management APIs, AppHost registration.

## Steps
- [x] **Step 1 — Business analysis and technical planning**
  - [x] Impact map: new service, own database, OpenIddict entities
  - [x] Write the technical blueprint
  - [x] Resolve clarifying questions (UI, endpoints, orchestration, refactor)
  - [x] Blueprint approved by the user
- [x] **Step 2 — Implementation**
  - [x] New OpenIdentity.App project with EF Core, ASP.NET Identity and OpenIddict
  - [x] Own database for users, roles, tokens and authorizations
  - [x] Razor login/register UI plus the /connect/* endpoints
  - [x] Management APIs (clients, resources, roles) as Minimal APIs
  - [x] Register the service in the Aspire AppHost
- [x] **Step 3 — Verification and quality**
  - [x] Code review completed
  - [x] All 10 code-review fixes applied (security, architecture, quality)
  - [x] Secrets removed from DbInitializer
- [x] **Step 4 — Documentation, memory and completion**
  - [x] Walkthrough written
  - [x] Conventional commit created
  - [x] Findings promoted into project memory

## Links
- Branch: feature/open-identity
- Commits:
- PRs:
- Files: src/Services/OpenIdentity.App, src/Mango.AppHost/Program.cs
- Docs: docs/architecture/overview.md
- Memory: .agent/memory/domains/identity.md

## Decisions
- `OpenIdentity.App` runs **in parallel with** `Identity.API` rather than replacing it — different
  ports, different databases, no conflict at startup. Choosing between them at runtime came later,
  in `IDENTITY-TOGGLE-001`.
- It serves its own Razor login/register pages, mirroring `Identity.API`'s custom UI, rather than
  scaffolding the default ASP.NET Identity UI.
- Management APIs (clients, resources, roles) were written as **Minimal APIs** with admin-role
  protection, to match the repo's Minimal-API-only convention.
- The service is registered directly in the Aspire AppHost.

## Gotchas
- The `mango` scope name had to be used (not `api`) so existing microservice authorization policies
  would accept OpenIddict-issued tokens without change.
- Secrets must not be hardcoded in `DbInitializer` — this was raised in code review and fixed.

## Open Questions
None.

## Blockers
None were opened.

## Session Log

### 2026-03-11
Service implemented, reviewed, and all ten code-review findings fixed. See
`artifacts/code-review.md` and `artifacts/walkthrough.md` in this directory.
