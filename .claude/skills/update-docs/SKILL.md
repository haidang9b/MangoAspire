---
name: update-docs
description: Use when the user runs /update-docs to bring /docs back in sync with code that just landed.
disable-model-invocation: true
---

# Update Docs

Keep `/docs` accurate against the current Vertical Slice backend and React frontend after a change
lands. `/docs` is **human documentation only**: agent memory lives in `.agent/memory/`, ticket state
in `.agent/tickets/` and feature specs in `specs/`. Never write any of those into `/docs`.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Code changes | No | Git diff, PR or file list. Defaults to the current branch's changes versus `master`. |
| Ticket/PR | No | Reference to link for traceability. |

## Workflow

1. **Scan the changes:** the given diff, or `git diff master...HEAD`.
2. **Identify what moved:**
   - **Backend:** Minimal API endpoints, handler logic, EF Core entities.
   - **Frontend:** components, hooks, Context changes.
3. **Map to the target doc.** A changed area updates its existing section; only a genuinely new
   pattern gets a new file.

   | Area | Target |
   |---|---|
   | Architecture | `docs/architecture/` and `docs/ARCHITECTURE.md` |
   | Backend | `docs/backend/` and `docs/api/` |
   | Frontend | `docs/frontend/` |
   | Infrastructure / database | `docs/infrastructure/` |

4. **Preview.** Show the user the proposed changes ("updating X in Y") and wait for approval.
5. **Apply** the approved edits. Use .NET 11 / C# 15 terminology, Minimal APIs (not Controllers) and
   the existing Context providers (no new state library), and link the ticket or PR. If the code's
   intent is too ambiguous to document accurately, ask instead of guessing.
6. **Hand off:** suggest the `generate-commit` skill for a `docs(...)` commit.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Duplicate docs | Check the target map before creating a file. |
| Hallucinated logic | Ask rather than guess unclear intent. |
| Writing agent memory into /docs | Memory goes in `.agent/memory/`, specs in `specs/`. |
| Missing traceability | Link the ticket or PR. |
