---
id: TICKET-ID
kind: refactor
status: not_started
priority: normal
createdUtc: 1970-01-01T00:00:00Z
updatedUtc: 1970-01-01T00:00:00Z
completedUtc: null
tags: []
---

<!--
kind: feature | bug | chore | security | refactor | spike
status: not_started | in_progress | awaiting_approval | blocked | completed | abandoned
State lives in this file only. A step is done only when every task under it is checked or
struck through as skipped. Do not maintain progress anywhere else.
-->

# TICKET-ID — One-line title, no markdown

## Plan
*Approved by the user before Step 2 starts.*

### Motivation
*What's wrong with the current shape, and why fixing it now is worth the churn.*

### Before / after
*The current structure vs. the target structure, concretely (files, boundaries, patterns).*

### Risk and rollback
*What could break as a side effect, and how to back out if it does.*

### Steps
*The concrete steps, in order.*

### Verification
*How you'll confirm behavior is unchanged (existing tests, build, lint).*

## Steps
- [ ] **Step 1 — Scope and plan**
  - [ ] Define motivation and the before/after shape
  - [ ] Identify risk and rollback strategy
  - [ ] Write the plan above
  - [ ] Plan approved by the user
- [ ] **Step 2 — Execute**
  - [ ] Carry out the refactor
- [ ] **Step 3 — Verification**
  - [ ] Build/tests green with behavior unchanged
  - [ ] Zero compiler warnings (fix-warnings)
- [ ] **Step 4 — Documentation, memory and completion**
  - [ ] Sync `/docs`, if the refactor changes documented structure
  - [ ] Conventional commit created
  - [ ] Promote Decisions/Gotchas below into `.agent/memory/domains/`

## Links
- Branch:
- Commits:
- PRs:
- Files:
- Docs:
- Memory:

## Decisions
## Gotchas
## Open Questions
## Blockers
*One `### blk-N — <one-line summary> (severity: low|medium|high, opened: YYYY-MM-DD, resolved: YYYY-MM-DD|open)` heading per blocker.*

## Session Log
*Append-only, newest last. One `### YYYY-MM-DD` heading per working session.*
