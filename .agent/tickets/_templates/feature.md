---
id: TICKET-ID
kind: feature
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

### Requirement
*The PO requirement in one paragraph, plus acceptance criteria as a plain list.*

### Backend
- **Slice:** which service and `Features/<FeatureName>/` folder
- **Command/Query + Handler:** names and the `ResultModel<T>` payload
- **Validator:** the FluentValidation rules
- **Route:** the Minimal API endpoint in `Routes/` (kebab-case, REST noun)
- **Persistence:** `DbContext` configuration and whether an EF Core migration is required
- **Integration events:** published or handled, if any

### Frontend
- **Components:** new or changed components under `src/UI/mango-ui/src/`
- **API client:** the call added to `src/UI/mango-ui/src/api/`
- **State:** which existing context is reused (`AuthContext`, cart, theme, notification)
- **Types:** the TypeScript interfaces that must match the backend DTOs

### Touchpoints
*Exact files this ticket will modify.*

### Risks and open questions
*What could break, and what needs a decision from the user.*

## Steps
- [ ] **Step 1 — Business analysis and technical planning**
  - [ ] Run the analyze-requirement sub-workflow
  - [ ] Context search: identify backend and frontend touchpoints
  - [ ] Write the technical blueprint above
  - [ ] Present clarifying questions to the user
  - [ ] Blueprint approved by the user
- [ ] **Step 2 — Implementation (full-stack)**
  - [ ] Backend slice: command/query, handler, validator, DTOs, route
  - [ ] Persistence: DbContext configuration and EF Core migration
  - [ ] Frontend: React UI and api/ client calls
  - [ ] Sync TypeScript interfaces with backend DTOs
- [ ] **Step 3 — Verification and quality**
  - [ ] Backend tests written and `dotnet test` passing
  - [ ] `pnpm lint` and `pnpm build` passing for the SPA
  - [ ] Zero compiler warnings (fix-warnings)
- [ ] **Step 4 — Documentation, memory and completion**
  - [ ] Sync `/docs` with the implementation
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
*Why this approach. Promoted into `.agent/memory/domains/*.md` when the ticket completes.*

## Gotchas
*Non-obvious traps discovered while implementing. Also promoted to memory on completion.*

## Open Questions
*Unresolved with the user or the codebase. Delete each line once answered.*

## Blockers
*One `### blk-N — <one-line summary> (severity: low|medium|high, opened: YYYY-MM-DD, resolved: YYYY-MM-DD|open)` heading per blocker, with detail below it.*

## Session Log
*Append-only, newest last. One `### YYYY-MM-DD` heading per working session.*
