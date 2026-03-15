---
name: onboard-analyzer
description: Performs a deep-dive analysis of a React + .NET 10 codebase to generate an onboarding documentation pack.
---

# Full-Stack Codebase Onboard Analyzer

Help a new engineer understand this codebase by analyzing both the React frontend and .NET 10 backend, producing an actionable, read-first onboarding documentation pack securely in `/docs`.

## When to Use

- When detecting a new or undocumented React + .NET 10 repository.
- When generating or updating an onboarding documentation pack (`/docs`).
- When explicitly triggered by `/onboard`, `/analyze-fe`, or `/analyze-be` commands.

## When Not to Use

- When modifying existing codebase logic (this skill operates strictly in a read-only execution mode).
- For performing any destructive commands (e.g., file deletes, DB resets - never run these without explicit confirmation).
- When attempting to bypass permissions for required dependency installations (`npm install` or `dotnet restore`).

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Trigger Command | Optional | Specifically `/onboard` (full scan), `/analyze-fe` (frontend only), or `/analyze-be` (backend only). Defaults to `/onboard` if unspecified. |

## Workflow

### Step 1: Inventory & Initial Survey (Phase A)
If running a full `/onboard` or starting analysis:
1. Map key topological paths: Identify `src/*`, `src/UI`, `apps`, `package.json`, `.sln`, and `.csproj` structures using read-only tools.
2. Generate `docs/onboarding/questions.md`:
   - Identify missing business logic documentation, auth stories, or secret handling processes.
   - If multiple Node or .NET SDK versions exist, explicitly ask the user for their preferred versions.
   - Pause and explicitly ask clarifying questions if critical configuration files (like `appsettings.json` or `package.json`) are missing.

### Step 2: Frontend Analysis (React 19) (Phase B)
If `/onboard` or `/analyze-fe`:
1. Detect stack dependencies: Identify patterns like Vite/Next.js, TypeScript usage, Tailwind, and State Management (RTK/React Query).
2. Generate `docs/frontend/overview.md` highlighting Routing architecture, data-fetching layers, and error-handling strategies.
3. Generate `docs/frontend/dependencies.md` mapping the dependency graph and highlighting outdated packages.
*Use preferred commands if permitted by the user:*
1. `pnpm i` \| `yarn install` \| `npm ci` (prefer `pnpm` if `pnpm-lock.yaml` exists, else `yarn`, else `npm`)
2. `pnpm test -r` \| `yarn test` \| `npm test`

### Step 3: Backend Analysis (.NET 10) (Phase C)
If `/onboard` or `/analyze-be`:
1. Detect stack patterns: Minimal APIs vs Controllers, Clean Architecture layers, EF Core data patterns, and testing tools (xUnit unless NUnit/MSTest is detected).
2. Generate `docs/backend/overview.md` outlining Middleware pipelines, API surface representations, and Persistence strategies.
3. Generate `docs/backend/dependencies.md` calculating the project reference graph.
*Use preferred commands if permitted by the user:* `dotnet restore` → `dotnet build` → `dotnet test --logger "trx;LogFileName=test_results.trx"`

### Step 4: Cross-Cutting Architecture and Quality (Phase D)
1. Map Frontend ↔ Backend interactions: Evaluate DTO definitions, CORS configurations, and JWT Authentication flows across the stack. Output findings to `docs/architecture/overview.md`.
2. Evaluate Testing Hotspots: Run `dotnet test` and frontend test commands (only if explicitly permitted) to identify flaky tests. Output to `docs/quality/hotspots.md`.

### Step 5: Conventions & Action Plan (Phase E)
1. Consolidate Guidelines: Summarize application folder structures, Dependency Injection patterns, and naming conventions. Output to `docs/conventions.md`.
2. Propose Action Plan: Create `docs/onboarding/next-steps.md` with realistic "First 5 PRs" ideas for a newly onboarded engineer.

## Validation

- [ ] A `/docs` directory is populated with `conventions.md` and related output folders (`backend`, `frontend`, `onboarding`, `architecture`, `quality`).
- [ ] No destructive commands were invoked during the analysis whatsoever.
- [ ] Required clarifying questions were correctly paused on if `appsettings.json` or other vital elements were missing.
- [ ] The generated documentation outputs correctly represent architectural choices aligned with React + .NET 10.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Missing critical info halts progress without notice. | Explicitly pause, output clarifying questions indicating missing `appsettings.json` or `package.json` info, and wait for user response. |
| Making assumptions about frontend package managers. | Strictly prefer `pnpm` if `pnpm-lock.yaml` exists, then `yarn`, else fallback to `npm`. |
| Assumed permission to restore packages or test. | Ask for explicit confirmation before running restorative or executing (`npm install`, `dotnet restore`, `npm test`, `dotnet test`) actions over the codebase. |
| Assuming test runners in .NET. | Always default assumptions to `xUnit` testing conventions unless `NUnit` or `MSTest` are explicitly found. |
