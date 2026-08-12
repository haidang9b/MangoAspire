# OPENIDENTITY-001 — Notes

> State lives in `ticket.json`. This ticket predates the tracker convention, so its steps were
> reconstructed from `plan.md` and the two artifacts below during the migration to `.agent/tickets/`.

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
