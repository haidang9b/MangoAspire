---
name: onboard-analyzer
description: Performs a deep-dive analysis of a React + .NET 10 codebase to generate an onboarding documentation pack.
---

# Full-Stack Codebase Onboard Analyzer

Generate a read-first onboarding pack under `/docs` for MangoAspire (React/Vite SPA + .NET 10,
Vertical Slice backend — see `AGENTS.md` for the fixed stack facts; this skill documents structure
and conventions on top of them, it doesn't re-detect the stack). Read-only: never run destructive
commands, and never run installs/builds/tests without explicit permission — **`pnpm`, not `npm`**,
for the SPA.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Trigger | No | `/onboard` (full), `/analyze-fe`, or `/analyze-be`. Defaults to full. |

## Workflow

1. **Survey:** map `src/`, `src/UI/mango-ui`, `.sln`/`.csproj` layout. Write
   `docs/onboarding/questions.md` for anything genuinely missing (business logic, auth flows,
   secret handling) — pause and ask rather than guess.
2. **Frontend** (`/onboard` or `/analyze-fe`): write `docs/frontend/overview.md` (routing,
   data-fetching, error handling) and `docs/frontend/dependencies.md`. If permitted:
   `pnpm --dir src/UI/mango-ui install`, then lint/build to sanity-check.
3. **Backend** (`/onboard` or `/analyze-be`): write `docs/backend/overview.md` (middleware
   pipeline, API surface, persistence) and `docs/backend/dependencies.md` (project reference
   graph). If permitted: `dotnet restore` → `dotnet build` → `dotnet test`.
4. **Cross-cutting:** frontend↔backend contracts (DTOs, CORS, JWT auth) into
   `docs/architecture/overview.md`; flaky-test hotspots (only with permission to run tests) into
   `docs/quality/hotspots.md`.
5. **Conventions & next steps:** consolidate folder structure/DI/naming into
   `docs/conventions.md`; propose a "first 5 PRs" list in `docs/onboarding/next-steps.md`.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Silently stuck on missing info | Pause, write the question to `docs/onboarding/questions.md`, wait for the user. |
| Wrong package manager | `pnpm` only for the SPA — never invent an `npm`/`yarn` command. |
| Running installs/tests unasked | Confirm before any restorative or executing command. |
| Re-detecting a fixed stack | The stack (VSA, xUnit, pnpm) is fixed — see `AGENTS.md`; document conventions, don't re-guess the stack. |
