---
description: End-to-end execution of a feature or bug fix from requirements to documentation.
---

# Workflow: implement-ticket
Description: End-to-end execution of a feature or bug fix from requirements to documentation.

## Initialization: Memory & State Tracking
1. **Initialize Tracker:** Before starting, create or read `docs/tracking/TICKET-ID.md`. 
   - If the file exists, **resume** execution from the first unchecked `[ ]` step.
   - If new, generate a standardized task checklist for Steps 1-4.
2. **Load Memory:** Read `docs/memory/project-context.md` (or equivalent global memory file) to load previous architectural constraints, known bugs, or project-specific "gotchas" before making any decisions.

### Step 1: Business Analysis and Technical Planning
1. **Trigger BA Analysis:** Execute the sub-workflow `analyze-po-requirement`.
   - The agent acts as a BA to translate PO requirements into a Technical Blueprint.
   - Output: A structured list of Entities, API Contracts, and UI Components.
2. **Context Search:** Scan relevant Backend (Domain/Application) and Frontend (Components/Context) files to identify exact touchpoints.
3. **Draft Plan & Track:** Update `docs/plans/TICKET-ID.md` based on BA analysis containing:
   - **Backend:** Proposed Minimal API endpoints, DTOs, and Result Pattern logic.
   - **Frontend:** Proposed React 19 Components, Context Provider updates, and React Query hooks.
   - **Persistence:** Required EF Core migrations or Repository changes.
4. **Clarify:** Present "Clarifying Questions" from the BA phase to the user.
5. **Pause & Update State:** - Update tracking state: `Step 1: Pending User Approval`.
   - Wait for the user to answer questions and type "Approve" to proceed.
   - Once approved, mark Step 1 as `[x] Completed` in the tracker.

### Step 2: Implementation (Full-Stack)
*Agent must update the `docs/tracking/TICKET-ID.md` checklist after completing each sub-step below.*
1. **Backend:** Implement Domain and Application logic using C# 14 Primary Constructors. Mark `[x] Backend Domain` in tracker.
2. **Infrastructure:** Update Repositories and EF Core configurations in the Infrastructure layer. Mark `[x] Infrastructure` in tracker.
3. **Frontend:** Implement React 19 UI using Tailwind CSS. Integrate data fetching with React Query. Mark `[x] Frontend UI` in tracker.
4. **State Management:** Update relevant Context Providers (e.g., Auth, Notification) if global state is affected.
5. **Sync:** Ensure TypeScript interfaces strictly match Backend DTOs. Mark Step 2 `[x] Completed`.

### Step 3: Verification and Quality
1. **Backend Testing:** Run 'create-unit-test' to create xUnit tests with Moq and Shouldly. Execute `dotnet test`.
2. **Frontend Testing:** Generate Vitest tests using React Testing Library. Execute `npm run test`.
3. **Refactor:** Run the 'fix-warnings' workflow to ensure zero C# 14 compiler warnings.
4. **Update Tracker:** If tests pass, mark Step 3 `[x] Completed`. If failed, update tracker to `Blocked: Test Failure` and propose a fix.

### Step 4: Documentation, Memory, and Completion
1. **Update Docs:** Run 'manage-documentation' to sync `/docs` with the new implementation.
2. **Commit:** Trigger 'generate-commit' to create a Conventional Commit.
3. **Consolidate Memory:** Extract any new architectural decisions, workarounds, or domain learnings discovered during this ticket and append them to `docs/memory/project-context.md` for future context.
4. **Archive:** Move the plan (`docs/plans/TICKET-ID.md`) and the completed tracker (`docs/tracking/TICKET-ID.md`) to `docs/archive/plans/` and mark the ticket `[x] Done`.

---

## Safety, Constraints & Memory Management
- **Gatekeeper:** Do not start coding until the BA Blueprint is approved by the user.
- **Resumability:** If the workflow stops or crashes, the agent must check `docs/tracking/TICKET-ID.md` to resume exactly where it left off.
- **Onion Integrity:** If the BA analysis suggests logic in the API layer, the agent must propose moving it to the Domain/Application layer.
- **Fail Fast:** If any test fails in Step 3, the workflow stops, logs the failure in the tracker, and waits until a fix is proposed.