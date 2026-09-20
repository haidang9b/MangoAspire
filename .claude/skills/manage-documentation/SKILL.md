---
name: manage-documentation
description: Analyzes code changes to synchronize or create technical documentation in the /docs folder for a React + .NET 10 codebase.
---

<!-- GENERATED from .agent/ by scripts/sync-agent-harness.ps1 - do not edit directly -->
# Documentation Manager

Keep `/docs` accurate against the current Vertical Slice backend and React frontend. Use after a
feature/change lands, or when a ticket requires docs to be synced. Not for writing/refactoring code
itself, and not for a read-only codebase survey with no intent to update `/docs` (use
`analyze-codebase` or `onboard-analyzer` instead).

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Code changes | No | Git diff/PR/file list to analyze. Defaults to scanning recent workspace changes. |
| Target ticket/PR | No | Reference to link for traceability. |

## Workflow

1. **Scan changes** — the given diff, or recent workspace additions/modifications.
2. **Identify what moved:**
   - Backend: new Minimal API endpoints, handler logic, EF Core entities.
   - Frontend: new React components, hooks, Context/state changes.
3. **Draft or update** — new pattern → new file in the right `/docs` subfolder; changed existing
   code → update the existing section (e.g. `docs/api/endpoints.md`) rather than duplicating.

   | Area | Target |
   |---|---|
   | Architecture | `docs/architecture/overview.md` |
   | Backend | `docs/backend/services.md`, `docs/api/endpoints.md` |
   | Frontend | `docs/frontend/components.md`, `docs/frontend/state-management.md` |
   | Database | `docs/infrastructure/database-schema.md` |

4. **Quality:** correct .NET 10/C# 14 terminology; link the ticket/PR; if the code's intent is too
   ambiguous to document accurately, stop and ask rather than guess.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Duplicate docs | Check the target-doc map before creating a new file. |
| Hallucinated logic | Ask the user instead of guessing unclear intent. |
| Outdated terminology | Minimal APIs, not Controllers; existing Context providers, not a new state library. |
| Missing traceability | Link the ticket/PR in the doc. |
