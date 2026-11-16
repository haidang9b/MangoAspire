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
- [x] **Step 2 — Execute**
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
  - [x] Task 11: Author evals for six skills and run the first benchmark
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
Follow-ups from the benchmark (AC-10). Every skill whose verdict isn't `helps` gets one. Whether to act
in this ticket or a new one is the spec's open question for the user.

- **Four of the five model-invocable skills are neutral.** create-unit-test, run-tests,
  generate-commit and add-feature-slice score the same without the skill, because `AGENTS.md` and the
  path-scoped rules already carry their conventions. Making them user-only would cut the 758-char
  skill listing to code-review's alone. Decide whether to make them user-only, retire them, or write
  harder evals first.
- **add-feature-slice** (neutral, 0.93 / 0.93):
  - Non-discriminating: all 14 other expectations (slice layout, `AsNoTracking` + `Select`,
    `ISender.SendAsync`, the validator, `DataVerificationException`, `IEventBus.PublishAsync`, builds).
  - The one failure, in both configs, is an eval defect. Eval 3 looks for `ProductDeletedIntegrationEvent`
    under `src/Services/Products.API/**`, but shared events live in `src/Shared/Mango.Events/<Area>/`,
    and both sessions put it there (verified from their diffs). Fix the glob; the corrected check
    passes both configs, so Δ stays 0.00.
  - Health: 34 denials (mostly compound `dotnet … | Select-Object` commands) and 2 max-turn cut-offs,
    both without the skill.
  - Next: harder evals, such as a new API project with AppHost wiring, which only the skill's
    references cover.
- **analyze-requirement** (neutral, 0.89 / 0.89):
  - Non-discriminating: the AC format, the Specs lint, the coupon and cancellation ACs, and asking open
    questions about the vague requirement.
  - Both configs fail eval 3's "Does not invent acceptance criteria for points the requirement leaves
    undecided": each wrote 12–13 ACs for its own reading of "faster". In the first run the with-skill
    config passed it, so the skill's guard is weak, not absent.
  - Next: tighten the skill to stop at Open questions when the outcome itself is undefined. It is
    user-only, so being neutral costs no session context.
  - Health: 13 denials and 1 max-turn cut-off (eval 2, with the skill).
- **create-unit-test** (neutral, 1.00 / 1.00): all 15 expectations are non-discriminating, and the
  `backend-testing` rule already loads for test files. Add evals for what only the skill states, or
  make it user-only.
- **generate-commit** (neutral, 1.00 / 1.00): all 13 expectations are non-discriminating. Same options.
- **run-tests** (neutral, 1.00 / 1.00): all 14 expectations are non-discriminating. The first run's
  `helps` (+0.17) came from denied `dotnet test` calls, not from the skill. Same options.
- Still pending from the spec: is a 0.10 Δ the right bar for `helps`? It is the default in use.

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

### 2026-11-16 — Task 11 benchmark (clean run)
Results: `tests/skill-evals/_results/20261116-185611` (gitignored), 36 sessions at ref `a8e4ae8`,
1 run per config, $30.52, about 95 min.

| Skill | Evals | With skill | Without | Δ | Verdict | Cost (USD) | Denials | Max-turns | Errors |
|---|---|---|---|---|---|---|---|---|---|
| add-feature-slice | 3 | 0.93 | 0.93 | 0.00 | neutral | 8.31 | 34 | 2 | 0 |
| analyze-requirement | 3 | 0.89 | 0.89 | 0.00 | neutral | 10.57 | 13 | 1 | 0 |
| code-review | 3 | 1.00 | 0.85 | +0.15 | helps | 4.12 | 3 | 0 | 0 |
| create-unit-test | 3 | 1.00 | 1.00 | 0.00 | neutral | 4.69 | 23 | 0 | 0 |
| generate-commit | 3 | 1.00 | 1.00 | 0.00 | neutral | 1.42 | 1 | 0 | 0 |
| run-tests | 3 | 1.00 | 1.00 | 0.00 | neutral | 1.41 | 0 | 0 | 0 |

code-review helps on two expectations:
- eval 1 "Ranks that finding as Critical". It failed with the skill in the first run; the
  `references/rules.md` fix in `8e8c2d5` made the skill pass it.
- eval 3 "Does not present problems in code outside the diff as findings about this change".

Follow-ups for the neutral skills are under Open Questions.

How the benchmark got here (about $68 of evals in all):
- **First full run** (`20261116-163734`, ref `1183f1b`, $30.58): invalid for 4 of 6 skills.
  - The allowlist was Bash-only, but sessions on Windows run `dotnet` through the PowerShell tool,
    so every build and test was denied.
  - One unparseable grader reply gave create-unit-test a false `hurts`.
  - Fixed in `1c85f8c`.
- **Partial re-run** (`20261116-182019`, 12 sessions, $6.25): stopped after the final review.
  - Prompts reached `claude` as US-ASCII, and diffs were decoded through the console codepage.
  - analyze-requirement's file check matched this branch's own spec.
  - No run health was recorded.
  - Fixed in `6b98682`.
- **Clean run:** above, after a smoke run (`20261116-185459`) of the fixed pipeline.

### 2026-11-16 — Close-out (Tasks 11–12, final review)
| Measure | Before | After |
|---|---|---|
| Always-on context (`CLAUDE.md` + imports + rules without `paths`) | 12,730 B | 3,656 B |
| Skill listing | 15 entries, 1,246 description chars | 5 model-invocable, 758 chars, plus 5 user-only |
| Duplicate rule text | 13.6 KB | 0 |
| Rules auto-loading by path | none | Claude `.claude/rules/` + Copilot `.github/instructions/` |
| Guidance checked against the code | nothing | `-Check`: 10 lint categories in 1.7 s, run in CI |

- Pester: 92/92 on Windows PowerShell 5.1. `-Check`: OK.
- The Facts lint removed false claims:
  - `Coupon.API`, which doesn't exist;
  - MediatR APIs (the mediator is in-house);
  - `IRepository`;
  - RTK and MUI, which the SPA doesn't use.
- The final review caught one false claim that Facts can't, because the token is real:
  "`WarningsAsErrors` is on" (only CS1998/CS4014 are errors).
- Final review (fresh context, opus): 0 Critical, 6 Important (all fixed, TDD), 16 Minor
  (13 deferred, 3 fixed as re-graded or unfinished plan steps).
- Also found and fixed while gathering evidence: `-Check` exited 0 on exactly one finding under
  Windows PowerShell (`48d5151`).
- Still open before `completed`:
  - AC-3, AC-4 and AC-12 (the VS Code hand-check and the first CI run);
  - AC-8 (no `/implement-ticket` run on a feature ticket yet);
  - Portability (PowerShell 7 on Linux runs only in CI);
  - push, PR and merge.
