---
name: implement-ticket
description: Use when the user runs /implement-ticket to carry a ticket from spec and plan through implementation, AC verification and documentation.
disable-model-invocation: true
---

# Implement Ticket

End-to-end execution of a ticket: spec and plan → implementation → AC verification → docs, memory
and commit.

## State contract

- **One file is the state:** `.agent/tickets/<TICKET-ID>/ticket.md`.
  - The front-matter (`kind`, `status`, `priority`, timestamps) is the header.
  - `## Steps` checkboxes are progress.
  - `## Plan`, `## Decisions`, `## Gotchas`, `## Open Questions`, `## Blockers` and `## Session Log` are prose.
- **Create or resume a ticket** for any multi-step task, not only when this skill is invoked.
  - **New:** pick `kind` (`feature | bug | chore | security | refactor | spike`) and copy
    `.agent/tickets/_templates/<kind>.md`. Fill `id` (= directory name), the title, `createdUtc` and
    `updatedUtc`, set `status: in_progress`, and set `activeTicketId` in `.agent/state/current.json`.
  - **Existing:** resume at the first `- [ ]` step, then the first unchecked task inside it, scanning
    top to bottom. `- [x] ~~title~~ — skipped: <reason>` counts as closed.
- **After every state change,** edit `ticket.md` directly: check the box, update `updatedUtc`,
  append to the Session Log if it's worth remembering.
- **Load memory progressively:** `.agent/memory/index.md` first, then only the domain files it
  lists for this area.

## Step 1 — Spec and plan

1. **`feature` tickets:** read and follow `.claude/skills/analyze-requirement/SKILL.md`. It writes
   `specs/<slug>/` (spec, acceptance criteria, test plan) from `specs/_template/`. Record
   `specs/<slug>/` under `## Links` → Docs. Other kinds draft `## Plan` from their template
   sections; they write a spec only if they change user-facing behaviour.
2. **Context search:** the backend `Features/` and `Routes/` slices and the frontend components and
   Context that this touches. Record them under `## Links` → Files.
3. **Clarify:** present the open questions; log unanswered ones under `## Open Questions`.
4. **Pause:** set `status: awaiting_approval` and wait. On approval, check the Step 1 tasks, set
   `status: in_progress`, and set the spec's `**Status:** approved`.

## Step 2 — Implementation

Follow `## Steps` → Step 2 and the spec's ACs. Check each task's box as you go.

- **Vertical Slice Architecture:** no Domain/Application/Repository layers; `DbContext` directly in
  handlers; no cross-slice calls. Templates are in the `add-feature-slice` skill.
- **Pipeline:** Logging → Validation → Tx behaviours. Throw `DataVerificationException` for
  business errors (handled by `GlobalExceptionHandler`).
- Keep `Program.cs` thin (`AddApiDefaults()` / `UseApiPipeline()` / `MigrateDatabaseAsync()`).

## Step 3 — Verification

1. **Backend:** use the `create-unit-test` skill for new or changed logic, then
   `dotnet test MangoAspire.sln` (or the affected `*.Tests` project with `--filter`).
2. **Frontend:** there is no test runner. Run `pnpm --dir src/UI/mango-ui lint` and `build`.
3. **Warnings:** read and follow `.claude/skills/fix-warnings/SKILL.md` until the build is clean
   (`WarningsAsErrors` is on).
4. **ACs:** for each `AC-n` in `specs/<slug>/acceptance-criteria.md`, tick it `[x]` **only** with
   cited evidence (a test name, or a command plus its output line). List every unmet AC.
5. **On failure** (a failing test or an unmet AC): set `status: blocked`, add
   `### blk-N — <summary> (severity: …, opened: <date>, resolved: open)` under `## Blockers` with
   the repro, and stop for instructions.

## Step 4 — Docs, memory, completion

1. **Docs:** if `/docs` is affected, read and follow `.claude/skills/update-docs/SKILL.md`. `/docs` is
   human documentation; never write memory, ticket state or specs there. Record the files under
   `## Links` → Docs.
2. **Commit:** use the `generate-commit` skill and record the hash under `## Links` → Commits.
3. **Memory:** promote durable, non-obvious `## Decisions` / `## Gotchas` into
   `.agent/memory/domains/`. Add any new domain file to `.agent/memory/index.md` in the same edit,
   and follow `.agent/memory/MEMORY_GUIDE.md`.
4. **Complete:** set the spec's `**Status:** shipped`, the ticket's `status: completed` and
   `completedUtc`, and clear `activeTicketId`. Nothing is moved; `status: completed` is the archive.

## Safety

- **Gatekeeper:** do not start Step 2 until the plan (and spec) is approved.
- **Skipping is explicit:** strike a task through with `— skipped: <reason>`; never check it silently.
- **Fail fast:** a failing test or an unmet AC stops the workflow with a recorded blocker.
