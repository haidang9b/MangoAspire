---
description: End-to-end execution of a feature or bug fix from requirements to documentation.
---

# Workflow: implement-ticket
Description: End-to-end execution of a feature or bug fix from requirements to documentation.

## Initialization: State & Memory

1. **Resolve the ticket home.** Everything for a ticket is one file: `.agent/tickets/<TICKET-ID>/ticket.md`.
   Create or resume a ticket for **any** multi-step task — not only when this command is explicitly
   invoked. If you're about to touch several files or take more than a couple of tool calls and no
   ticket covers it, stop and create one first.
   - **New ticket:** pick a `kind` — `feature` (new user-facing capability), `bug` (something is
     broken), `chore` (maintenance/tooling/infra, no user-facing behavior change), `security`
     (vulnerability/hardening/dependency work), `refactor` (internal restructuring, no behavior
     change), or `spike` (exploratory/feasibility). Copy
     `.agent/tickets/_templates/<kind>.md` to `.agent/tickets/<TICKET-ID>/ticket.md`. Fill the
     front-matter `id` (must equal the directory name), the title heading, `createdUtc`,
     `updatedUtc`, and set `status: in_progress`. Set `activeTicketId` in `.agent/state/current.json`.
   - **Existing ticket:** read `ticket.md` and **resume** — under `## Steps`, take the first
     `- [ ]` step, then the first unchecked task inside it. Scan top to bottom; never assume "last
     checked + 1". A skipped task is written as `- [x] ~~title~~ — skipped: <reason>` — treat it as
     closed, not as work to do.
2. **Load memory progressively.** Read `.agent/memory/index.md` first, then read **only** the domain
   files it lists as relevant to this ticket's area. Do not read all of `.agent/memory/`.
3. **State contract — do not violate it:** `ticket.md` is the only place state lives. The
   front-matter (`status`, `priority`, timestamps) is the header; `## Steps` checkboxes are
   progress; `## Plan`, `## Decisions`, `## Gotchas`, `## Open Questions`, `## Blockers`,
   `## Session Log` are prose. There is nothing to keep in sync across files because there is only
   one file — that's the point.
4. **After every state change:** edit `ticket.md` directly (check the box, update `updatedUtc`,
   append a line if worth remembering). There is no generated artifact to regenerate and nothing
   else to run.

### Step 1: Planning
1. **`feature` tickets only:** run the `analyze-requirement` sub-workflow (BA analysis against a PO
   requirement) and write its output into `## Plan`. Every other kind skips this — draft `## Plan`
   directly from the kind's template sections (already present in the file) without BA/PO framing.
2. **Context Search:** scan the relevant feature slices (backend `Features/` + `Routes/`) and
   frontend (`src/UI/mango-ui` components/context) to identify exact touchpoints. Record them under
   `## Links` → Files.
3. **Clarify:** present open questions to the user; log unanswered ones under `## Open Questions`.
4. **Pause:** set `status: awaiting_approval` in the front-matter. Wait for the user to answer and
   approve. Once approved, check the step-1 tasks and set `status: in_progress`.

### Step 2: Implementation
*Check the matching task's box in `## Steps` after each sub-step below.* For a `feature` ticket this
is full-stack (backend slice + persistence + pipeline/errors + frontend + state + type sync, as
already itemized in the template). For every other kind, it's whatever `## Steps` → Step 2 says —
follow the plan, not a fixed shape.
- **Vertical Slice Architecture** — no separate Domain/Application/Repository layers.
- **Pipeline & errors:** requests flow through `LoggingBehavior -> ValidationBehavior -> TxBehavior`;
  throw `DataVerificationException` for business/data errors (handled by `GlobalExceptionHandler`).

### Step 3: Verification and Quality
1. **Backend Testing:** run the `create-unit-test` skill for new/changed logic. Execute
   `dotnet test MangoAspire.sln` (or the affected `*.Tests` project/`--filter`).
2. **Frontend Checks:** no frontend test runner exists — run `pnpm --dir src/UI/mango-ui lint` and
   `pnpm --dir src/UI/mango-ui build` instead.
3. **Refactor:** run the `fix-warnings` workflow for a clean build (`WarningsAsErrors` is on).
4. **On failure:** set `status: blocked`, add a `### blk-N — <one-line summary> (severity: ...,
   opened: <date>, resolved: open)` heading under `## Blockers` with the repro/stack trace, and stop
   for instructions.

### Step 4: Documentation, Memory and Completion
1. **Update Docs:** run `manage-documentation` to sync `/docs` with the change, if applicable.
   `/docs` is **human documentation** — never write agent memory or ticket state there. Record what
   you touched under `## Links` → Docs.
2. **Commit:** run `generate-commit`. Record the hash under `## Links` → Commits.
3. **Consolidate Memory:** promote `## Decisions` / `## Gotchas` into the right file under
   `.agent/memory/domains/`. If no existing domain fits, create one **and add its line to
   `.agent/memory/index.md` in the same edit**. Record the files under `## Links` → Memory. Apply
   the add/update threshold in `.agent/memory/MEMORY_GUIDE.md` — durable, non-obvious facts only.
4. **Complete:** set `status: completed` and `completedUtc`, clear `activeTicketId` in
   `.agent/state/current.json`. **Nothing is moved or archived** — `status: completed` is the
   archive; the ticket stays where it is.

---

## Safety, Constraints & Memory Management
- **Gatekeeper:** do not start Step 2 until `## Plan` is approved by the user.
- **Resumability:** if the workflow stops or crashes, read `ticket.md` and resume at the first
  unchecked step/task, top to bottom.
- **Skipping is explicit:** a task that turns out to be unnecessary is checked and struck through
  with `— skipped: <reason>` — never silently checked without a reason. A step is only fully closed
  when every task under it is checked (done or skipped).
- **Architecture Integrity:** Vertical Slice Architecture by feature. Keep `Program.cs` thin (wire
  via `AddApiDefaults()` / `UseApiPipeline()` / `MigrateDatabaseAsync()`). Do **not** introduce the
  Repository pattern or layered controllers/services unless maintainers explicitly request it.
- **Fail Fast:** if any test fails in Step 3, the workflow stops, records a blocker, and waits.
