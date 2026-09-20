---
id: TICKET-ID
kind: bug
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

### Repro
*Exact steps, inputs, and environment that reproduce the failure. Observed vs. expected behavior.*

### Root cause
*What is actually wrong, traced to the specific code/config responsible — not just the symptom.*

### Fix
*The minimal change that addresses the root cause. Note the affected slice(s)/file(s).*

### Regression test
*What test proves this stays fixed, and where it lives.*

### Risks and open questions
*What else could be affected, and what needs a decision from the user.*

## Steps
- [ ] **Step 1 — Reproduce and diagnose**
  - [ ] Confirm repro steps and capture the failing behavior
  - [ ] Identify root cause
  - [ ] Write the fix plan above
  - [ ] Fix plan approved by the user
- [ ] **Step 2 — Fix**
  - [ ] Implement the fix in the affected slice(s)
  - [ ] Add or update a regression test that fails before the fix and passes after
- [ ] **Step 3 — Verification and quality**
  - [ ] Affected `dotnet test` / `pnpm lint`+`build` passing
  - [ ] Zero compiler warnings (fix-warnings)
- [ ] **Step 4 — Documentation, memory and completion**
  - [ ] Sync `/docs` with the fix, if applicable
  - [ ] Conventional commit created
  - [ ] Promote the root cause below to `.agent/memory/domains/`, if non-obvious

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
