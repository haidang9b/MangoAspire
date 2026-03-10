# Ticket Tracker: [TICKET-ID]

**Title**: [Brief Title of Feature or Bug]
**Current Status**: [Not Started | In Progress | Pending User Approval | Blocked | Completed]
**Last Updated**: [YYYY-MM-DD HH:MM]

> **Agent Instructions:** > - Update the `Current Status` and `Last Updated` fields whenever you pause or finish a step.
> - Change `[ ]` to `[x]` as you complete each task. Do not skip tasks.
> - If an error occurs (e.g., test failure), change `Current Status` to `Blocked`, log the error in the "Active Blockers" section, and wait for instructions.

---

## Step 1: Business Analysis and Technical Planning
- [ ] Run `analyze-requirement` sub-workflow.
- [ ] Perform Context Search (identify touchpoints in Backend & Frontend).
- [ ] Generate Technical Blueprint at `docs/plans/[TICKET-ID].md`.
- [ ] Present Clarifying Questions to the user.
- [ ] **PAUSE**: Wait for User Approval.
- [ ] Blueprint Approved by user.

## Step 2: Implementation (Full-Stack)
- [ ] **Backend**: Implement Domain/Application logic (C# 14 Primary Constructors).
- [ ] **Infrastructure**: Update Repositories / EF Core configurations.
- [ ] **Frontend**: Implement React 19 UI (Tailwind, React Query).
- [ ] **State Management**: Update Context Providers (if applicable).
- [ ] **Sync**: Ensure TypeScript interfaces match Backend DTOs.

## Step 3: Verification and Quality
- [ ] **Backend Tests**: Run `test-gen`, execute `dotnet test` (All Pass).
- [ ] **Frontend Tests**: Generate Vitest tests, execute `npm run test` (All Pass).
- [ ] **Refactor**: Run `fix-warnings` to ensure zero C# 14 compiler warnings.

## Step 4: Documentation, Memory, and Completion
- [ ] **Docs**: Run `manage-docs` to sync `/docs` folder.
- [ ] **Commit**: Run `generate-commit` for Conventional Commit message.
- [ ] **Memory**: Append findings from "Memory Scratchpad" to `docs/memory/project-context.md`.
- [ ] **Archive**: Move this tracker and the blueprint to `docs/archive/plans/`.

---

## Active Blockers / Notes
*Log test failures, unresolved questions, or reasons for pausing here.*
- [Empty]

## Memory Scratchpad
*Log architectural decisions, workarounds, or domain learnings discovered during this ticket. These will be consolidated into the global memory file in Step 4.*
- [Empty]