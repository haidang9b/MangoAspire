# AI-native harness

**Status:** approved
**Owner:** Dang Phan
**Last updated:** 2026-11-16

## Problem
The agent harness costs too many tokens and is only partly trusted.

- Every Claude Code session loads about 12.7 KB of always-on instructions.
- The skill listing shows 15 entries, 5 of which do the same job as another entry.
- Rules never load automatically in either tool, and two rule files are verbatim copies.
- Copilot sees only a fraction of the guidance.
- Nothing checks that AI-written guidance is true. A rule mandates MUI, which the SPA doesn't use; memory describes a `Coupon.API` project that doesn't exist; and `AGENTS.md` names MediatR, which isn't a dependency.
- Nobody can say whether a skill actually improves the agent's output.

## User & outcome
**Developers using Claude Code or GitHub Copilot in MangoAspire.** Sessions start lean. Conventions
appear when you touch matching files. Each skill exists once and is read natively by both tools.
CI fails when guidance drifts from the code or exceeds its budget.

**Maintainers of the harness.** Each skill's value can be measured, with and without the skill, as a
pass-rate delta. Feature work follows the agentic-devkit spec format (`spec.md`,
`acceptance-criteria.md`, `test-plan.md`).

## Scope
### In
- `.claude/` as the canonical home for rules, skills and agents, with a generated Copilot mirror in
  `.github/instructions` and `.github/agents`.
- Merging duplicate skills, workflows and agents; making human-run workflows user-only.
- Path-scoped rules replacing `.agent/rules`, `CODING_CONVENTIONS.md` and `API_PROJECT_STRUCTURE.md`.
- Slimming `AGENTS.md` and `CLAUDE.md` to an index.
- A harness lint run in CI: budgets, rules, skills, agents, links, staleness, drift, **facts**,
  **specs** and **evals**.
- The agentic-devkit spec format under `specs/`, used by `/implement-ticket` and `/analyze-requirement`.
- Skill output evals: a with-skill vs without-skill runner, graded, with per-skill pass-rate deltas.
  It covers 6 skills, with at least 3 evals each.

### Out
- The agentic-devkit workflow commands (`/capture-idea`, `/new-feature`, `/implement`,
  `/review-code`, `/ship`) and its `planning/` folders.
- Skill **trigger** evals (precision/recall of skill selection).
- Reviewing AI-generated product code against ACs, and the Completeness/Correctness/Leverage
  report (agentic-devkit `analyze-fx-efficiency`).
- Codex, Antigravity and `.agents/skills` targets; `.github/copilot-instructions.md`.
- Human documentation under `docs/`.
- The project-level `superpowers` plugin setting.

## Constraints & assumptions
- The dev machine has Windows PowerShell 5.1 and no `pwsh`; CI runs Linux `pwsh`. Every script must
  run on both.
- Skill evals call the `claude` CLI, which costs tokens and needs auth, so they run on demand, never
  in CI.
- Vendor behaviour was checked on 2026-11-16 (see `design.md` §2). "Copilot coding agent reads
  `.claude/skills/`" comes from secondary sources and is confirmed by a manual check.

## Open questions
- [ ] Is a 0.10 pass-rate delta the right bar for calling a skill "helps"? — owner: Dang Phan
- [ ] Should skills that come out `neutral` or `hurts` be rewritten in this ticket or in a follow-up? — owner: Dang Phan

## Related
- Design and investigation: [design.md](design.md)
- Implementation plan: [plan.md](plan.md)
- Ticket: [../../.agent/tickets/HARNESS-NATIVE-001/ticket.md](../../.agent/tickets/HARNESS-NATIVE-001/ticket.md)
- Memory index: [../../.agent/memory/index.md](../../.agent/memory/index.md)
