---
description: End-to-end execution of a feature or bug fix from requirements to documentation.
---

# Workflow: implement-ticket
Description: End-to-end execution of a feature or bug fix from requirements to documentation.

## Execution Steps

### Step 1: Business Analysis and Technical Planning
1. **Trigger BA Analysis:** Execute the sub-workflow `analyze-requirement`.
   - The agent acts as a BA to translate PO requirements into a Technical Blueprint.
   - Output: A structured list of Entities, API Contracts, and UI Components.
2. **Context Search:** Scan relevant Backend (Domain/Application) and Frontend (Components/Context) files to identify exact touchpoints.
3. **Draft Plan:** Create a temporary file `docs/plans/TICKET-ID.md` based on the BA analysis containing:
   - **Backend:** Proposed Minimal API endpoints, DTOs, and Result Pattern logic.
   - **Frontend:** Proposed React 19 Components, Context Provider updates, and React Query hooks.
   - **Persistence:** Required EF Core migrations or Repository changes.
4. **Clarify:** Present the "Clarifying Questions" from the BA phase to the user.
5. **Pause:** Wait for the user to answer questions and type "Approve" to proceed.

### Step 2: Implementation (Full-Stack)
1. **Backend:** Implement Domain and Application logic using C# 14 Primary Constructors.
2. **Infrastructure:** Update Repositories and EF Core configurations in the Infrastructure layer.
3. **Frontend:** Implement React 19 UI using Tailwind CSS. Integrate data fetching with React Query.
4. **State Management:** Update relevant Context Providers (e.g., Auth, Notification) if global state is affected.
5. **Sync:** Ensure TypeScript interfaces strictly match Backend DTOs.

### Step 3: Verification and Quality
1. **Backend Testing:** Run 'test-gen' to create xUnit tests with Moq and Shouldly. Execute `dotnet test`.
2. **Frontend Testing:** Generate Vitest tests using React Testing Library. Execute `npm run test`.
3. **Refactor:** Run the 'fix-warnings' workflow to ensure zero C# 14 compiler warnings.

### Step 4: Documentation and Completion
1. **Update Docs:** Run 'manage-docs' to sync `/docs` with the new implementation.
2. **Commit:** Trigger 'generate-commit' to create a Conventional Commit.
3. **Archive:** Move the plan from Step 1 to `docs/archive/plans/`.

## Safety and Constraints
- **Gatekeeper:** Do not start coding until the BA Blueprint is approved by the user.
- **Onion Integrity:** If the BA analysis suggests logic in the API layer, the agent must propose moving it to the Domain/Application layer.
- **Fail Fast:** If any test fails in Step 3, the workflow stops until a fix is proposed.