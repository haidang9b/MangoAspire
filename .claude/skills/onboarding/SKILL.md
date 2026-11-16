---
name: onboarding
description: Use when the user runs /onboarding to generate or refresh the onboarding pack under /docs and get a Day-1 guide.
disable-model-invocation: true
---

# Onboarding

Generate or refresh a read-first onboarding pack under `/docs` for MangoAspire (React/Vite SPA +
.NET 11 Vertical Slice backend), then give the developer a Day-1 guide. The stack is fixed (see
`AGENTS.md`): document structure and conventions on top of it rather than re-detecting it.

Read-only by default: never run destructive commands, and run installs, builds or tests only with
explicit permission. The SPA uses **`pnpm`**; never use any other package manager.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Scope | No | `full` (default), `frontend` or `backend`. |

## Outputs

Each output is created or refreshed by this skill; write only those in scope:

```text
docs/onboarding/questions.md      open questions for the team (never guess)
docs/frontend/overview.md         routing, data fetching, error handling
docs/frontend/dependencies.md
docs/backend/overview.md          middleware pipeline, API surface, persistence
docs/backend/dependencies.md      project reference graph
docs/architecture/overview.md     frontend-backend contracts (DTOs, CORS, JWT auth)
docs/quality/hotspots.md          flaky-test hotspots (only with permission to run tests)
docs/conventions.md               folder structure, DI, naming
docs/onboarding/next-steps.md     "first 5 PRs"
```

## Workflow

1. **Survey.** Map `src/`, `src/UI/mango-ui` and the `.sln`/`.csproj` layout. Read `README.md`,
   `/docs` and `.agent/memory/index.md`, then follow the memory links for the developer's area.
   Write genuinely missing facts (business logic, auth flows, secret handling) to the questions
   file, and pause to ask rather than guess.
2. **Frontend** (full or frontend): write the frontend overview and dependencies. If permitted:
   `pnpm install --dir src/UI/mango-ui`, then `pnpm --dir src/UI/mango-ui lint` and `build`.
3. **Backend** (full or backend): write the backend overview and dependencies. If permitted:
   `dotnet restore`, `dotnet build`, `dotnet test`.
4. **Cross-cutting:** write the architecture overview and, with permission to run tests, the hotspots.
5. **Conventions and next steps:** write the conventions file and the first-PRs list.
6. **Day-1 guide.** Summarize for the developer:
   - how to run it (`scripts/run-app.ps1`);
   - where guidance lives (`AGENTS.md`, `.claude/rules/`, `.claude/skills/`);
   - what is in flight (`.agent/tickets/`, the `status` in each `ticket.md` front-matter);
   - how feature specs work (`specs/`).

   Then ask which part of the stack they want to explore first.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Silently stuck on missing info | Write the question to the questions file and wait. |
| Wrong package manager | `pnpm` only for the SPA. |
| Running installs or tests unasked | Confirm before any restore, build or test command. |
| Re-detecting a fixed stack | The stack (VSA, xUnit, pnpm) is fixed; document conventions. |
