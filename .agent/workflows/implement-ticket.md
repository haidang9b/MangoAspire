---
description: End-to-end execution of a feature or bug fix from requirements to documentation.
---

# Workflow: implement-ticket
Description: End-to-end execution of a feature or bug fix from requirements to documentation.

## Initialization: State & Memory

1. **Resolve the ticket home.** Everything for a ticket lives in `.agent/tickets/<TICKET-ID>/`.
   - **New ticket:** copy `.agent/tickets/_template/` to it, then fill `id` (must equal the
     directory name), `title`, `createdUtc`, `updatedUtc`, and set `status: "in_progress"`. Set
     `activeTicketId` in `.agent/state/current.json`.
   - **Existing ticket:** read `ticket.json` and **resume** — take the first step whose `done` is
     `false`, then the first task in that step whose `done` is `false` and `skipped` is not `true`.
     Scan in order; never assume "last done + 1". Read `notes.md` before deciding anything.
2. **Load memory progressively.** Read `.agent/memory/index.md` first, then read **only** the domain
   files it lists as relevant to this ticket's area. Do not read all of `.agent/memory/`.
3. **State contract — do not violate it:**
   - `ticket.json` owns status, step/task flags, timestamps, blockers and links. It is the single
     source of truth for "where are we".
   - `notes.md` owns narrative: decisions, gotchas, open questions, blocker detail, session log.
     **Never write a status line or a `- [ ]` checkbox there.**
   - `plan.md` is the approved blueprint. It records intent, not progress.
4. **After every state change:** update `ticket.json` (the flag *and* `updatedUtc`), append to
   `notes.md` if anything is worth remembering, then run
   `pwsh ./scripts/update-ticket-board.ps1`.

### Step 1: Business Analysis and Technical Planning
1. **Trigger BA Analysis:** Execute the sub-workflow `analyze-requirement`.
   - The agent acts as a BA to translate PO requirements into a Technical Blueprint.
   - Output: A structured list of Entities, API Contracts, and UI Components.
2. **Context Search:** Scan the relevant feature slices (backend `Features/` + `Routes/`) and frontend (`src/UI/mango-ui` components/context) to identify exact touchpoints. Record them in `links.files`.
3. **Draft Plan:** Write `.agent/tickets/<TICKET-ID>/plan.md` based on the BA analysis containing:
   - **Backend:** Proposed Minimal API endpoints (in `Routes/`), commands/queries + handlers, DTOs, FluentValidation validators, and `ResultModel<T>` logic — organized as **vertical slices by feature**.
   - **Frontend:** Proposed React components, Context Provider updates, and `src/UI/mango-ui/src/api/` client calls consumed through hooks.
   - **Persistence:** Required EF Core migrations / `DbContext` configuration changes.
4. **Clarify:** Present "Clarifying Questions" from the BA phase to the user. Log the unanswered ones under `## Open Questions` in `notes.md`.
5. **Pause & Update State:**
   - Set `status: "awaiting_approval"` in `ticket.json` and regenerate the board.
   - Wait for the user to answer the questions and approve.
   - Once approved, set the step-1 tasks `done` and `status` back to `"in_progress"`.

### Step 2: Implementation (Full-Stack)
*Set the matching task's `done` flag in `ticket.json` after each sub-step below, then regenerate the board.*
1. **Backend slice:** Implement the command/query, handler, validator, and DTOs inside the feature slice (Vertical Slice Architecture — no separate Domain/Application/Repository layers). Register the endpoint in `Routes/`.
2. **Persistence:** Update `DbContext` configuration and add an EF Core migration if the schema changed. Use EF Core projections + `AsNoTracking()` for read paths.
3. **Pipeline & errors:** Ensure requests flow through the `LoggingBehavior -> ValidationBehavior -> TxBehavior` pipeline; throw `DataVerificationException` for business/data errors (handled centrally by `GlobalExceptionHandler`).
4. **Frontend:** Implement the React UI, wiring API calls in `src/UI/mango-ui/src/api/` through hooks/components. Reuse existing contexts (`AuthContext`, cart/theme/notification).
5. **State Management:** Update relevant Context Providers if global state is affected.
6. **Sync:** Ensure TypeScript interfaces strictly match backend DTOs.

### Step 3: Verification and Quality
1. **Backend Testing:** Run the `create-unit-test` skill to create xUnit tests with Moq and Shouldly. Execute `dotnet test MangoAspire.sln` (or the affected `*.Tests` project/`--filter`).
2. **Frontend Checks:** There is **no frontend test runner** configured for `src/UI/mango-ui`. Instead run `pnpm --dir src/UI/mango-ui lint` and `pnpm --dir src/UI/mango-ui build` to verify the frontend.
3. **Refactor:** Run the `fix-warnings` workflow to ensure a clean build (`WarningsAsErrors` is enabled in `Directory.Build.props`).
4. **On failure:** set `status: "blocked"`, append a `blockers[]` entry with a **one-line** `summary`
   and `resolvedUtc: null`, write the repro and stack trace under a `### blk-N` heading in
   `notes.md`, regenerate the board, and stop for instructions.

### Step 4: Documentation, Memory, and Completion
1. **Update Docs:** Run `manage-documentation` to sync `/docs` with the new implementation. `/docs` is **human documentation** — never write agent memory or ticket state there. Record what you touched in `links.docs`.
2. **Commit:** Trigger `generate-commit` to create a Conventional Commit. Record the hash in `links.commits`.
3. **Consolidate Memory:** Promote the `## Decisions` and `## Gotchas` sections of `notes.md` into
   the right file under `.agent/memory/domains/`. If no existing domain fits, create a new file
   **and add its line to `.agent/memory/index.md` in the same edit**. Record the files you touched
   in `links.memory`. Apply the add/update threshold in `.agent/memory/MEMORY_GUIDE.md` — durable,
   non-obvious facts only; one-off task detail stays in `notes.md`.
4. **Complete:** set `status: "completed"` and `completedUtc`, clear `activeTicketId` in
   `.agent/state/current.json`, and regenerate the board. **Nothing is moved or archived** —
   `status: "completed"` is the archive.

---

## Safety, Constraints & Memory Management
- **Gatekeeper:** Do not start coding until the BA Blueprint is approved by the user.
- **Resumability:** If the workflow stops or crashes, read `.agent/tickets/<TICKET-ID>/ticket.json` and resume at the first step/task that is not `done` and not `skipped`.
- **Skipping is explicit:** a task that turns out to be unnecessary gets `"skipped": true` plus a `skipReason` — never a silent `done: true`. A step is `done` only when every task under it is done or skipped.
- **Architecture Integrity:** Follow **Vertical Slice Architecture** by feature. Keep `Program.cs` thin (wire via `AddApiDefaults()` / `UseApiPipeline()` / `MigrateDatabaseAsync()`). Do **not** introduce the Repository pattern or layered controllers/services unless maintainers explicitly request it.
- **Fail Fast:** If any test fails in Step 3, the workflow stops, records a blocker, and waits until a fix is proposed.
