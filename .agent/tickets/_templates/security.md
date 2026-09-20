---
id: TICKET-ID
kind: security
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

### Threat / goal
*What vulnerability, exposure, or hardening gap this addresses, and its severity.*

### Scope
*What's in and explicitly what's out.*

### Steps
*The concrete steps, in order, with the files/areas each one touches.*

### Verification
*The scan/audit/test command that proves the finding is resolved and nothing new was introduced.*

### Risks and open questions
*What could break, and what needs a decision from the user.*

## Steps
- [ ] **Step 1 — Scope and plan**
  - [ ] Define the vulnerability/hardening goal and scope
  - [ ] Write the plan above
  - [ ] Plan approved by the user
- [ ] **Step 2 — Execute**
  - [ ] Carry out the planned changes
- [ ] **Step 3 — Verification**
  - [ ] Affected build/lint/tests passing
  - [ ] Security check clean (scan/audit shows no new findings)
  - [ ] Zero compiler warnings (fix-warnings), if code changed
- [ ] **Step 4 — Documentation, memory and completion**
  - [ ] Sync `/docs`, if applicable
  - [ ] Conventional commit created
  - [ ] Promote Decisions/Gotchas below into `.agent/memory/domains/`, if non-obvious

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
