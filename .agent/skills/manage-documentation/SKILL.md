---
name: manage-documentation
description: Analyzes code changes to synchronize or create technical documentation in the /docs folder for a React + .NET 10 codebase.
---

# Documentation Manager

Maintain a set of accurate, up-to-date Markdown documents that reflect the current state of the Clean Architecture backend and the React frontend. This skill ensures that code changes are continuously synchronized with the project's technical documentation.

## When to Use

- After completing a new feature or implementing a major change in a React + .NET 10 codebase.
- When pull requests or tickets mandate that documentation must be updated to match the code.
- To automatically generate missing documentation for newly created architectural elements (e.g., Minimal APIs, React hooks).

## When Not to Use

- When writing code or refactoring logic (this skill is exclusively for documentation maintenance).
- When a codebase analysis is needed without the intent of updating the `/docs` folder (use `analyze-codebase` or `onboard-analyzer` instead).

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Code Changes | No | The specific Git diff, PR, or list of modified files to analyze. Defaults to scanning the workspace for recent additions if not provided. |
| Target Ticket/PR | No | Ticket or PR reference to link in the documentation for traceability. |

## Workflow

### Step 1: Change Analysis
1. Scan the workspace for recent file additions or modifications.
2. If provided as input, narrow the scope of analysis strictly to the proposed Code Changes or Git diff.

### Step 2: Context Identification
1. **Backend Analysis:** Detect new .NET 10 Minimal API endpoints, Application Service logic, or Entity Framework Core entities.
2. **Frontend Analysis:** Detect new React components, custom hooks, or state management updates (e.g., Redux Toolkit slices or React Query mutations).

### Step 3: Drafting or Updating
1. **New Features:** If a fundamentally new feature or pattern is found, generate a new Markdown file in the appropriate `/docs` subfolder.
2. **Existing Code:** If existing code is modified, precisely update the corresponding documentation section rather than creating a new file (e.g., updating an API contract in `docs/api/endpoints.md`).

*Target Documentation Map:*
- **Architecture:** `docs/architecture/overview.md` (System-wide diagrams and flow).
- **Backend:** `docs/backend/services.md` and `docs/api/endpoints.md`.
- **Frontend:** `docs/frontend/components.md` and `docs/frontend/state-management.md`.
- **Database:** `docs/infrastructure/database-schema.md`.

### Step 4: Verification and Quality Constraints
1. **Technical Accuracy:** Ensure the documentation strictly uses correct .NET 10 and C# 14 terminology.
2. **Traceability:** Embed explicit links or references connecting the documentation changes to the specific ticket, PR, or feature.
3. **No Hallucinations:** If complex logic is too ambiguous to infer accurately, strictly halt and ask the user for a high-level summary before writing the documentation.

## Validation

- [ ] Technical documentation files within the `/docs` directory accurately reflect the recent code changes.
- [ ] Backend documentation covers Minimal APIs and Minimal API routing terminology.
- [ ] Frontend documentation covers appropriate React rendering, hooks, and state management logic.
- [ ] Documentation includes a reference to the related ticket/PR (if applicable).
- [ ] No fabricated features or hallucinated C# 14/.NET 10 terminology is present.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Creating duplicate documentation | Always search the `Target Documentation Map` for existing elements before blindly creating new files. |
| Hallucinating logic | If the code intent is unclear, stop and execute a clarifying question to the user instead of guessing. |
| Using outdated terminology | Strictly enforce .NET 10 and C# 14 nomenclature (e.g., Minimal APIs over Controllers if that is the established pattern). |
| Forgetting to link the PR | Ask for or embed the Target Ticket/PR in the documentation preamble for traceability. |
