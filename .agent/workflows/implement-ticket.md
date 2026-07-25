---
description: End-to-end execution of a feature or bug fix from requirements to documentation.
---

# Workflow: implement-ticket
Description: End-to-end execution of a feature or bug fix from requirements to documentation.

## Initialization: Memory & State Tracking
1. **Initialize Tracker:** Before starting, create or read `docs/tracking/TICKET-ID.md`.
   - If the file exists, **resume** execution from the first unchecked `[ ]` step.
   - If new, generate a standardized task checklist for Steps 1-4 (use `docs/tracking/TICKET-ID.md` as the template).
2. **Load Memory:** Read `docs/memory/project-context.md` to load previous architectural constraints, known bugs, or project-specific "gotchas" before making any decisions.

### Step 1: Business Analysis and Technical Planning
1. **Trigger BA Analysis:** Execute the sub-workflow `analyze-requirement`.
   - The agent acts as a BA to translate PO requirements into a Technical Blueprint.
   - Output: A structured list of Entities, API Contracts, and UI Components.
2. **Context Search:** Scan the relevant feature slices (backend `Features/` + `Routes/`) and frontend (`src/UI/mango-ui` components/context) to identify exact touchpoints.
3. **Draft Plan & Track:** Update `docs/plans/TICKET-ID.md` based on BA analysis containing:
   - **Backend:** Proposed Minimal API endpoints (in `Routes/`), commands/queries + handlers, DTOs, FluentValidation validators, and `ResultModel<T>` logic — organized as **vertical slices by feature**.
   - **Frontend:** Proposed React components, Context Provider updates, and `src/UI/mango-ui/src/api/` client calls consumed through hooks.
   - **Persistence:** Required EF Core migrations / `DbContext` configuration changes.
4. **Clarify:** Present "Clarifying Questions" from the BA phase to the user.
5. **Pause & Update State:** - Update tracking state: `Step 1: Pending User Approval`.
   - Wait for the user to answer questions and type "Approve" to proceed.
   - Once approved, mark Step 1 as `[x] Completed` in the tracker.

### Step 2: Implementation (Full-Stack)
*Agent must update the `docs/tracking/TICKET-ID.md` checklist after completing each sub-step below.*
1. **Backend slice:** Implement the command/query, handler, validator, and DTOs inside the feature slice (Vertical Slice Architecture — no separate Domain/Application/Repository layers). Register the endpoint in `Routes/`. Mark `[x] Backend` in tracker.
2. **Persistence:** Update `DbContext` configuration and add an EF Core migration if the schema changed. Use EF Core projections + `AsNoTracking()` for read paths. Mark `[x] Persistence` in tracker.
3. **Pipeline & errors:** Ensure requests flow through the `LoggingBehavior -> ValidationBehavior -> TxBehavior` pipeline; throw `DataVerificationException` for business/data errors (handled centrally by `GlobalExceptionHandler`).
4. **Frontend:** Implement the React UI, wiring API calls in `src/UI/mango-ui/src/api/` through hooks/components. Reuse existing contexts (`AuthContext`, cart/theme/notification). Mark `[x] Frontend UI` in tracker.
5. **State Management:** Update relevant Context Providers if global state is affected.
6. **Sync:** Ensure TypeScript interfaces strictly match backend DTOs. Mark Step 2 `[x] Completed`.

### Step 3: Verification and Quality
1. **Backend Testing:** Run the `create-unit-test` skill to create xUnit tests with Moq and Shouldly. Execute `dotnet test MangoAspire.sln` (or the affected `*.Tests` project/`--filter`).
2. **Frontend Checks:** There is **no frontend test runner** configured for `src/UI/mango-ui`. Instead run `pnpm --dir src/UI/mango-ui lint` and `pnpm --dir src/UI/mango-ui build` to verify the frontend.
3. **Refactor:** Run the `fix-warnings` workflow to ensure a clean build (`WarningsAsErrors` is enabled in `Directory.Build.props`).
4. **Update Tracker:** If checks pass, mark Step 3 `[x] Completed`. If failed, update tracker to `Blocked: Test Failure` and propose a fix.

### Step 4: Documentation, Memory, and Completion
1. **Update Docs:** Run `manage-documentation` to sync `/docs` with the new implementation.
2. **Commit:** Trigger `generate-commit` to create a Conventional Commit.
3. **Consolidate Memory:** Extract any new architectural decisions, workarounds, or domain learnings discovered during this ticket and append them to `docs/memory/project-context.md` for future context.
4. **Archive:** Move the plan (`docs/plans/TICKET-ID.md`) and the completed tracker (`docs/tracking/TICKET-ID.md`) to `docs/archive/plans/` and mark the ticket `[x] Done`.

---

## Safety, Constraints & Memory Management
- **Gatekeeper:** Do not start coding until the BA Blueprint is approved by the user.
- **Resumability:** If the workflow stops or crashes, the agent must check `docs/tracking/TICKET-ID.md` to resume exactly where it left off.
- **Architecture Integrity:** Follow **Vertical Slice Architecture** by feature. Keep `Program.cs` thin (wire via `AddApiDefaults()` / `UseApiPipeline()` / `MigrateDatabaseAsync()`). Do **not** introduce the Repository pattern or layered controllers/services unless maintainers explicitly request it.
- **Fail Fast:** If any test fails in Step 3, the workflow stops, logs the failure in the tracker, and waits until a fix is proposed.
