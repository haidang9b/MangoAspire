---
id: HARNESS-OPT-001
kind: chore
status: in_progress
priority: normal
createdUtc: 2026-09-20T00:00:00Z
updatedUtc: 2026-09-20T02:00:00Z
completedUtc: null
tags: [harness, skills, tokens]
---

# HARNESS-OPT-001 — Token-optimize the .agent/ skills, workflows and agents

## Plan

### Goal
Cut the per-invocation token cost of the Claude Code harness sources (`.agent/skills/`,
`.agent/workflows/`, `.agent/agents/`) without changing what any of them trigger on or do. Part of
the broader dev-kit redesign; the ticket-tracking rebuild (single-file `ticket.md`, no
JSON/board) already landed separately.

### Scope
**In:** the 8 `.agent/skills/*/SKILL.md` files, the 7 `.agent/workflows/*.md` files, the 3
`.agent/agents/*.agent.md` files. Trim redundant/filler prose, collapse restated boilerplate
(e.g. "When to Use" sections that just repeat the frontmatter description), and fix the
conceptual staleness `sync-agent-harness.ps1` already flags (`Clean Architecture`,
`Repository pattern`, `React Query`, `npm install` — all wrong for this repo per `AGENTS.md`).
Push verbose reference material into a skill's `references/` folder (loaded on demand) where a
skill's body is carrying detail it doesn't need on every invocation.

**Out:** adding new skills/capabilities, the docs/ restructure (next phase), re-touching the
ticket-tracking rebuild.

### Steps
Read each file, rewrite for concision (no lost capability/trigger), regenerate `.claude/` via
`sync-agent-harness.ps1`, confirm the staleness-warning list comes back empty.

### Verification
`pwsh ./scripts/sync-agent-harness.ps1` runs clean with **no** staleness warnings, and `-Check`
afterward reports `.claude/` in sync with `.agent/`.

## Steps
- [x] **Step 1 — Scope and plan**
  - [x] Define goal and scope
  - [x] Write the plan above
  - [x] Plan approved by the user
- [x] **Step 2 — Execute**
  - [x] Skill: analyze-codebase — trimmed When-to-Use boilerplate, fixed "Clean Architecture" → VSA
  - [x] Skill: analyze-po-requirement — trimmed, fixed "Repository pattern" wording
  - [x] Skill: code-review — merged When-to/not-to-Use, dropped redundant Validation checklist
  - [x] Skill: create-unit-test — trimmed When-to-Use, tightened workflow steps
  - [x] Skill: generate-commit — cut ~60% (dropped generic git-command explanations the model
        already knows; kept the safety protocol and type table)
  - [x] Skill: manage-documentation — trimmed, fixed "React Query" → existing Context pattern
  - [x] Skill: onboard-analyzer — trimmed generic multi-repo stack detection (this repo's stack is
        fixed, see AGENTS.md); fixed "Clean Architecture" and "npm install" staleness
  - [x] Skill: run-tests — already lean (21 lines), left as-is
  - [x] Workflow: analyze-requirement — already lean, left as-is (only the ticket-model path edit
        from the earlier ticket-tracking rebuild)
  - [x] Workflow: fix-warnings — already lean, left as-is
  - [x] Workflow: implement-ticket — left as-is; already rewritten this session for the ticket.md
        model and appropriately detailed for a multi-step workflow
  - [x] Workflow: onboarding — already lean, left as-is
  - [x] Workflow: review — already lean, left as-is
  - [x] Workflow: test-gen — already lean, left as-is
  - [x] Workflow: update-docs — already lean, left as-is
  - [x] Agent: codebase-analyzer — cut duplication with its skill; fixed `.agents/skills` → `.agent/skills`
  - [x] Agent: code-reviewer — cut duplication with its skill; fixed `.agents/skills` → `.agent/skills`
  - [x] Agent: unit-test-creator — cut duplication with its skill (naming/usings/assertions were
        restated in full; now references the skill for the rule set)
- [x] **Step 3 — Verification**
  - [x] `sync-agent-harness.ps1` runs with zero staleness warnings
  - [x] `sync-agent-harness.ps1 -Check` reports in sync
- [ ] **Step 4 — Documentation, memory and completion**
  - [x] ~~Sync /docs~~ — skipped: this affects agent-facing harness sources, not `/docs` human docs
  - [ ] Conventional commit created — pending user go-ahead
  - [x] ~~Promote Decisions/Gotchas into .agent/memory/domains/~~ — skipped: these are harness/tooling
        facts (already documented inline in `.agent/tools/`), not project-domain knowledge; no
        domain file fits

## Links
- Branch:
- Commits:
- PRs:
- Files: .agent/skills/, .agent/workflows/, .agent/agents/
- Docs:
- Memory:

## Decisions
- **Applied `superpowers:writing-skills`'s SDO rule to all 8 skill `description` fields.** Every one
  of them violated it: they led with "what the skill does" instead of "Use when...", which is the
  exact anti-pattern that skill warns causes agents to follow the description instead of reading
  the skill body. Rewrote all 8 to be trigger-only, third person, "Use when..." — no workflow
  summary.
- **Skipped the writing-skills Iron Law's full RED-GREEN-REFACTOR pressure-testing.** These are
  established, already-working reference/technique skills for an in-house harness, not new
  discipline-enforcing skills — adversarial subagent pressure-testing is calibrated for the latter
  and would be disproportionate ceremony here. Applied the SDO correctness rules directly instead.
- Left the 8 already-lean workflow files (`analyze-requirement`, `fix-warnings`,
  `implement-ticket`, `onboarding`, `review`, `test-gen`, `update-docs`, and the `run-tests` skill)
  untouched rather than force a diff — none were flagged by the staleness scan and none carried
  redundant boilerplate.
- `generate-commit` got the deepest cut: it spelled out generic Conventional Commits knowledge and
  basic `git diff`/`git add` usage the model already has. Kept the type table (compact, genuinely
  useful as a lookup) and the Git Safety Protocol (repo-specific, safety-critical).

## Gotchas
- Two skills' frontmatter `name` didn't match their directory: `generate-commit/SKILL.md` said
  `name: generate-conventional-commit`, `run-tests/SKILL.md` said `name: run-project-tests`. The
  sync script silently forces `name` to the directory slug on generation, so `.claude/` was never
  wrong — but the `.agent/` source was, which is exactly the kind of drift this harness redesign is
  meant to eliminate. Fixed both at the source.
- Both `codebase-analyzer.agent.md` and `code-reviewer.agent.md` pointed at `.agents/skills/...`
  (wrong — the directory is `.agent/skills/`, singular). Fixed both, and added
  `.agents/skills` → `.agent/skills` to `sync-agent-harness.ps1`'s `$SafeReplacements` so this
  exact typo self-heals if it recurs.
- The sync script's staleness `$WarnPatterns` check is a dumb substring match with no negation
  awareness — writing "not Onion" or "no Repository pattern" still trips the warning on "Onion" /
  "Repository pattern" as literal substrings. Worked around by rephrasing rather than fighting the
  matcher (e.g. "not a layered N-Tier split", "no separate repository layer").

## Open Questions

## Blockers

## Session Log

### 2026-09-20
Opened to track the token-optimization pass requested alongside the ticket-tracking rebuild
(`ticket.json`/board removed in favor of single-file `ticket.md`, done separately). Proceeded
straight to Step 2 since scope was approved in conversation.

Rewrote 6 of 8 skills and all 3 agents for concision and fixed conceptual staleness
(`Clean Architecture`, `Repository pattern`, `React Query`, `npm install`) plus a real path bug
(`.agents/skills` → `.agent/skills`) in two agent files. Left the already-lean workflows and
`run-tests` untouched. `sync-agent-harness.ps1` ran clean with zero staleness warnings, and
`-Check` confirmed `.claude/` was in sync. This work landed in commit `2190af2`, committed outside
this session between turns.

### 2026-09-20 (continued)
User invoked `/superpowers:writing-skills` and asked for a correctness pass on the 8 skills, not
just token trimming. Applied the skill's own SDO rule to every `description` field (all 8 violated
"Use when..., trigger-only, no workflow summary") and fixed two `name`/directory mismatches
(`generate-commit`, `run-tests`). `sync-agent-harness.ps1` reran clean. Not yet committed.
