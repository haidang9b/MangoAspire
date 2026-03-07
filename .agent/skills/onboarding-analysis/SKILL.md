---
name: onboard-analyzer
description: Performs a deep-dive analysis of a React + .NET 10 codebase to generate an onboarding documentation pack.
# The agent will suggest running this when it detects a new or undocumented repository.
---

# Skill: Full‑Stack Codebase Onboarding & Analysis (React + .NET 10)

## Goal
Help a new engineer understand this codebase by analyzing both the React frontend and .NET 10 backend, producing an onboarding pack in `/docs`.

## Trigger Commands
- `/onboard` : Run the full analysis and generate all documentation.
- `/analyze-fe` : Run Phase B only.
- `/analyze-be` : Run Phase C only.

## Operating Mode
- **Read‑first:** Scan & summarize before changing anything.
- **Ask clarifying questions:** If `appsettings.json` or `package.json` are missing critical info, pause.
- **Tooling:** Use `ls`, `grep`, and `cat` to map the tree. If terminal access is granted, run `dotnet build` and `npm list`.

---

## Phase A — Inventory & Questions
1. **Map Key Paths:** Identify `src/*`, `src/UI`, `apps`, `package.json`, `.sln`, and `.csproj` files.
2. **Initial Report:** Generate `docs/onboarding/questions.md`. 
   - Identify missing business goals, auth stories, or secret handling.
   - Ask about preferred Node and .NET SDK versions if multiple versions are present.

## Phase B — Frontend Analysis (React 19)
1. **Detect Stack:** Vite/Next.js, TypeScript, Tailwind, and State Management (RTK/React Query).
2. **Output:** - `docs/frontend/overview.md` (Routing, data-fetching, error handling).
   - `docs/frontend/dependencies.md` (Highlight outdated packages).

## Phase C — Backend Analysis (.NET 10)
1. **Detect Stack:** Minimal APIs, Clean Architecture layers, EF Core, and xUnit/NSubstitute.
2. **Output:** - `docs/backend/overview.md` (Middleware, API surface, Persistence).
   - `docs/backend/dependencies.md` (Project reference graph).

## Phase D — Cross‑Cutting & Quality
1. **Architecture:** Map Frontend ↔ Backend interactions (DTOs, CORS, JWT Auth).
2. **Hotspots:** Run `dotnet test` and `npm test` (if allowed) to find flaky tests.
3. **Output:** `docs/quality/hotspots.md` and `docs/architecture/overview.md`.

## Phase E — Conventions & Next Steps
1. **Guidelines:** Summarize folder layout, DI patterns, and naming conventions into `docs/conventions.md`.
2. **Action Plan:** Create `docs/onboarding/next-steps.md` with "First 5 PRs" ideas.

---

## Safety & Constraints
- **Non-Destructive:** Do not delete files or reset git state.
- **Permissions:** If `npm install` or `dotnet restore` is required, ask for permission first.