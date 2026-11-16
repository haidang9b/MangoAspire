---
id: HARNESS-NATIVE-001
kind: chore
status: in_progress
priority: normal
createdUtc: 2026-11-16T00:00:00Z
updatedUtc: 2026-11-16T00:00:00Z
completedUtc: null
tags: [harness, skills, tokens, copilot, claude, specs, evals]
---

# HARNESS-NATIVE-001 — AI-native harness: token-lean, fact-checked, spec-driven, with skill evals

## Plan
*Approved by the user before Step 2 starts.*

### Goal
The spec is `specs/ai-native-harness/` (agentic-devkit format): `spec.md`,
`acceptance-criteria.md` (AC-1…AC-12), `test-plan.md`, plus `design.md` (investigation) and
`plan.md` (12 tasks, each citing its ACs).

### Scope
See `specs/ai-native-harness/spec.md` → Scope.

### Steps
See `specs/ai-native-harness/plan.md`.

### Verification
See `specs/ai-native-harness/test-plan.md`. The gate is `scripts/sync-agent-harness.ps1 -Check`
exiting 0 plus Pester passing (both in CI), the first eval benchmark recorded below, and the manual
checks in Claude Code and VS Code.

### Risks and open questions
See `spec.md` → Open questions and `test-plan.md` → Risks.

## Steps
- [x] **Step 1 — Scope and plan**
  - [x] Define goal and scope
  - [x] Write the spec, design and plan (`specs/ai-native-harness/`)
  - [x] Plan approved by the user
- [ ] **Step 2 — Execute**
  - [x] Task 1: Harness module — parsing, Copilot mirror, drift
  - [x] Task 2: Harness lint core and context report
  - [x] Task 3: Fact-check lint for AI-written content
  - [x] Task 4: Spec layer from agentic-devkit
  - [x] Task 5: Make `.claude/` canonical (mechanical move)
  - [x] Task 6: Consolidate skills and agents; make workflows spec-aware
  - [x] Task 7: Path-scoped rules
  - [x] Task 8: Lean always-on files
  - [x] Task 9: Skill eval engine
  - [x] Task 10: Skill eval runner
  - [ ] Task 11: Author evals for six skills and run the first benchmark
  - [x] Task 12: Docs, CI, close-out
- [ ] **Step 3 — Verification**
  - [x] `sync-agent-harness.ps1 -Check` exits 0
  - [x] Pester harness tests pass
  - [ ] Each AC ticked in `specs/ai-native-harness/acceptance-criteria.md` with evidence
  - [ ] Hand-check in Claude Code and VS Code Copilot
- [ ] **Step 4 — Documentation, memory and completion**
  - [x] ~~Sync `/docs`~~ — skipped: agent harness only, `/docs` is human documentation
  - [x] Conventional commits created (one or more per plan task)
  - [x] Promote Decisions/Gotchas into `.agent/memory/`, if non-obvious: the Windows PowerShell rules went to
    `.agent/tools/harness.md` and `.agent/tools/skill-evals.md`; the rest is in those docs already

## Links
- Branch: chore/HARNESS-NATIVE-001-ai-native-harness
- Commits: 9bbaaea..HEAD on the branch (one or more conventional commits per plan task; see `git log 15e0fdf..`)
- PRs:
- Files: AGENTS.md, CLAUDE.md, .claude/, .github/instructions/, .github/agents/, .agent/, specs/, scripts/harness/, scripts/eval-skills.ps1, tests/harness/, tests/skill-evals/
- Docs: specs/ai-native-harness/
- Memory:

## Decisions
- `.claude/` is canonical because Claude Code and VS Code Copilot both read `.claude/skills/`
  natively. Only `.github/instructions` and `.github/agents` are generated.
- Specs live in root `specs/<slug>/` in the agentic-devkit format, chosen by the user. The ticket stays the
  progress tracker. Only the spec format is adopted, not the devkit commands or `planning/`.
- Evaluation layers, chosen by the user: a deterministic fact-check in CI, and with-skill vs without-skill output
  evals on demand. Trigger evals and AC-vs-code effectiveness reports are out of scope.
- Evals live in `tests/skill-evals/`, outside the skill folders, so a with-skill session can't read
  the expected answers.

## Gotchas
- `powershell -File` passes `-Category A,B` as one string, so the scripts split on `,`.
- No `pwsh` on the dev machine (Windows PowerShell 5.1 + Pester 3.4). The plan installs Pester 5
  per user.
- PowerShell 5.1 mangles quotes in native arguments, so the eval runner pipes the prompt to `claude` on stdin.
- On Windows, `claude -p` sessions run commands through the **PowerShell** tool. `--allowedTools` needs
  `PowerShell(<prefix>:*)` next to every `Bash(<prefix>:*)`, or every build and test is denied.
- Windows PowerShell 5.1: `ConvertFrom-Json` emits a JSON array as one object, assigning an `if`
  statement unrolls a one-item array, and a lone object has no `.Count`. Wrap the whole expression in
  `@()` before reading `.Count`. Three bugs on this branch had this cause.

## Open Questions

## Blockers

## Session Log

### 2026-11-16
Investigated the harness and vendor docs. Measured always-on at 12,730 B, 13.6 KB of duplicate
rule text, rule frontmatter that no target tool reads, and 15 skill-listing entries containing 5
duplicate pairs. Found 3 factual errors in AI-written guidance: MUI/DataGrid in the frontend rule,
a `Coupon.API` project that doesn't exist, and MediatR, which isn't a dependency.

Then read `D:\Learning\agentic-devkit`: its spec templates, `check_kit.py`, `skill-creator` evals
and grader, and `analyze-fx-efficiency`. The user chose root `specs/`, spec format only, and the
fact-check plus output-eval layers. Wrote `specs/ai-native-harness/`. Awaiting review.

### 2026-11-16 — Task 3 Facts baseline (23 findings, 3.3 s)
Backtick-named symbols in harness files that are absent from the source corpus. Tasks 6–8 and 12 burn these down, by fixing the claim or allowlisting it with a reason:
  - `AGENTS.md` — `CreateCart`
  - `AGENTS.md` — `MethodName_When_Behavior_Then_ExpectedResult`
  - `AGENTS.md` — `ResultModel.Error`
  - `.claude/agents/unit-test-creator.md` — `Assert.X`
  - `.claude/skills/code-review/references/rules.md` — `sessionStorage`
  - `.claude/skills/code-review/references/rules.md` — `Task.Result`
  - `.claude/skills/code-review/references/rules.md` — `Task.Wait`
  - `.claude/skills/code-review/references/rules.md` — `_camelCase`
  - `.claude/skills/create-unit-test/SKILL.md` — `CalculateTotal_When_CartIsEmpty_Then_ReturnsZero`
  - `.claude/skills/create-unit-test/SKILL.md` — `action.ShouldThrow`
  - `.claude/skills/create-unit-test/SKILL.md` — `Assert.X`
  - `.agent/tools/harness.md` — `Vitest`
  - `.agent/tools/mcp-serena.md` — `Grep`
  - `.agent/memory/architecture.md` — `Mango.Core.Caching.ICacheManager`
  - `.agent/memory/architecture.md` — `Mango.Infrastructure.Caching.HybridCacheManager`
  - `.agent/memory/architecture.md` — `Coupon.API`
  - `.agent/memory/architecture.md` — `Mango.Common`
  - `.agent/memory/index.md` — `Coupon.API`
  - `.agent/memory/stack.md` — `Microsoft.AspNetCore.WebUtilities`
  - `.agent/memory/domains/checkout-saga.md` — `PaymentId`
  - `.agent/memory/domains/checkout-saga.md` — `CartItemsJson`
  - `.agent/memory/domains/identity.md` — `Mango.Core.Options.SeedUsersOptions`
  - `.agent/memory/domains/identity.md` — `RequireClaim`

### 2026-11-16 — Task 8 context report
`AlwaysOnBytes: 3548  ModelSkillCount: 5  UserSkillCount: 5  SkillListingChars: 758` (before: 12,730 B always-on; 15 listing entries, 1,246 chars of model-visible descriptions).
