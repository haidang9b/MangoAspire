---
name: "Docs: Generate or Update Documentation"
description: "Analyzes code changes to synchronize or create technical documentation in the /docs folder."
---

# Skill: Documentation Manager

## Goal
Maintain a set of accurate, up-to-date Markdown documents that reflect the current state of the Clean Architecture backend and the React frontend.

## Logic Flow
1. **Change Analysis:** Scan the workspace for recent file additions or modifications.
2. **Context Identification:**
   - **Backend:** Detect new Minimal API endpoints, Service logic, or Entity Framework entities.
   - **Frontend:** Detect new React components, custom hooks, or Redux Toolkit slices.
3. **Drafting/Updating:**
   - If a new feature is found: Generate a new Markdown file in the appropriate `/docs` subfolder.
   - If existing code is modified: Update the corresponding documentation section (e.g., updating an API contract in `docs/api/endpoints.md`).
4. **Verification:** Ensure that the documentation follows the project's architectural standards.

## Target Documentation Map
- **Architecture:** `docs/architecture/overview.md` (System-wide diagrams and flow).
- **Backend:** `docs/backend/services.md` and `docs/api/endpoints.md`.
- **Frontend:** `docs/frontend/components.md` and `docs/frontend/state-management.md`.
- **Database:** `docs/infrastructure/database-schema.md`.

## Quality Constraints
- **Technical Accuracy:** Use correct .NET 10 and C# 14 terminology.
- **Traceability:** Link documentation changes to the specific ticket or PR.
- **No Hallucinations:** If logic is too complex to infer, ask the user for a high-level summary.