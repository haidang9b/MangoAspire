# AI-native harness — design

Status: in-review, 2026-11-16. This is the investigation and target design behind [spec.md](spec.md);
[plan.md](plan.md) implements it. Targets: **Claude Code** and **GitHub Copilot** (VS Code, coding
agent, github.com).

## 1. What the harness costs today (measured)

| Area | Today | Problem |
|---|---|---|
| Always-on context (Claude Code) | 12,730 B ≈ 3.2K tokens: `CLAUDE.md` 2,741 + `AGENTS.md` 8,021 (imported) + `.agent/memory/index.md` 1,968 (imported) | `AGENTS.md` restates conventions held in four other files; `CLAUDE.md` restates `AGENTS.md` |
| Skill listing | 15 entries (8 skills + 7 commands), every description in context every turn | 5 pairs do one job: `review`/`code-review`, `test-gen`/`create-unit-test`, `update-docs`/`manage-documentation`, `onboarding`/`onboard-analyzer`, `analyze-requirement`/`analyze-po-requirement`; `analyze-codebase` overlaps the `codebase-analyzer` agent |
| Duplicate rule text | `rules/architecture.md` ≡ `API_PROJECT_STRUCTURE.md` (9 KB each); `rules/backend-dotnet.md` ≡ `CODING_CONVENTIONS.md` (4.4 KB each) | 13.6 KB read twice |
| Rule loading | Never automatic, in any tool | Antigravity-style `trigger: glob` frontmatter. Two files have prose in `globs:`; `frontend-react.md` has two frontmatter blocks and a typo path |
| Factual accuracy of AI-written guidance | Unchecked | `frontend-react.md` mandates MUI + DataGrid, which are not dependencies. `memory/architecture.md` describes a legacy `Coupon.API`, which exists nowhere in the repo. `AGENTS.md` names MediatR, which isn't in `Directory.Packages.props` (the repo has `src/Shared/Mediator`) |
| Skill quality | Unmeasured | No evals. Nobody can tell whether a skill beats no skill |
| Specs | None | Plans live as prose in `ticket.md`; no testable acceptance criteria |
| Copilot coverage | `AGENTS.md` and `.claude/skills/` only | No `.github/instructions/` or `.github/agents/` |
| Copies | `.agent/{skills,agents,workflows}` + generated `.claude/{skills,agents,commands}` | Every search hits each instruction twice |
| Tooling | Docs say `pwsh` | The dev machine has Windows PowerShell 5.1 + Pester 3.4 and no `pwsh`; `claude`, Python 3.12 and git are installed |

## 2. What each tool reads (checked against vendor docs, 2026-11-16)

| Artifact | Claude Code | VS Code Copilot | Copilot coding agent / github.com |
|---|---|---|---|
| Root instructions | `CLAUDE.md` + `@` imports; reads `AGENTS.md` itself only when no `CLAUDE.md` exists | `AGENTS.md`, `.github/copilot-instructions.md` | `AGENTS.md` |
| Path-scoped rules | `.claude/rules/**/*.md` + `paths:`; loads on Read/Write/Edit of a match. A missing or unparseable `paths` makes the rule always-on | `.github/instructions/**/*.instructions.md` + `applyTo:`; the Claude harness in VS Code also reads `.claude/rules` | `.github/instructions/` |
| Skills | `.claude/skills/<n>/SKILL.md`; only `description` is in context until invoked; `disable-model-invocation: true` takes it out, and `/n` still works | `.github/skills/`, `.claude/skills/`, `.agents/skills/`; honours `disable-model-invocation` | `.github/skills/`, `.claude/skills/` |
| Subagents | `.claude/agents/*.md` | `.github/agents/*.agent.md` | `.github/agents/*.agent.md` |
| Commands | Merged into skills; `.claude/commands/` is legacy | Prompt files | — |

Sources: [Claude memory](https://code.claude.com/docs/en/memory),
[Claude skills](https://code.claude.com/docs/en/skills),
[VS Code instructions](https://code.visualstudio.com/docs/agent-customization/custom-instructions),
[VS Code skills](https://code.visualstudio.com/docs/agent-customization/agent-skills),
[VS Code agents](https://code.visualstudio.com/docs/agent-customization/custom-agents),
[Copilot support matrix](https://docs.github.com/en/copilot/reference/custom-instructions-support),
[Agent Skills spec](https://agentskills.io/specification).

## 3. Principles

1. **Map, not manual.** Always-on files hold only what nearly every session needs, and they point to the rest.
2. **Pay only for what's relevant.** Rules load by file path, skills load by intent, and workflows load only when a human types the slash command.
3. **One canonical file per artifact**, in the location the most tools read natively. Generate only what a tool can't read from there.
4. **Trust, but verify mechanically.** AI-written guidance is fact-checked against the code in CI, and each skill's value is measured against a no-skill baseline.
5. **Spec before code** (from agentic-devkit). Testable ACs are the contract that verification and evals check against.
6. Normative (rules), descriptive (memory), contract (specs) and state (tickets) stay separate.

## 4. Target layout

```text
AGENTS.md                          always-on, cross-tool index (≤ 4 KB)
CLAUDE.md                          @AGENTS.md + Claude-only notes (≤ 1 KB)
.claude/
  rules/                           CANONICAL, path-scoped (paths:)
    backend.md backend-testing.md frontend.md docs.md specs.md
    memory/identity.md checkout-saga.md ui-web.md cart-checkout.md   pointers into .agent/memory/domains/
  skills/                          CANONICAL, read by Claude Code and Copilot
    code-review/ create-unit-test/ run-tests/ generate-commit/ add-feature-slice/      model-invocable (5)
    implement-ticket/ analyze-requirement/ onboarding/ update-docs/ fix-warnings/      user-only (5)
  agents/                          CANONICAL: code-reviewer, codebase-analyzer, unit-test-creator
.github/
  instructions/**/*.instructions.md  GENERATED from .claude/rules
  agents/*.agent.md                  GENERATED from .claude/agents
  workflows/build.yml                CI, plus a harness job
specs/
  _template/  spec.md acceptance-criteria.md test-plan.md   (ported from agentic-devkit)
  <slug>/     spec.md acceptance-criteria.md test-plan.md [design.md plan.md]
tests/
  harness/AgentHarness.Tests.ps1 SkillEval.Tests.ps1         Pester 5
  skill-evals/<skill>/evals.json  files/*.patch               eval definitions (outside skill dirs)
  skill-evals/_grader.md                                      grader prompt
  skill-evals/_results/                                       gitignored run output
scripts/
  sync-agent-harness.ps1           sync the .github mirror; -Check = full lint
  eval-skills.ps1                  run skill output evals
  harness/AgentHarness.psm1 SkillEval.psm1 facts-allowlist.txt
.agent/                            knowledge and state only: memory/ tickets/ tools/ state/ README.md
```

## 5. Mapping

### Skills (15 entries -> 10; model-visible descriptions 15 -> 5)

| After | Absorbs | Invocation |
|---|---|---|
| `code-review` | `review` workflow | model + `/` |
| `create-unit-test` | `test-gen` workflow | model + `/` |
| `run-tests` | — | model + `/` |
| `generate-commit` | — | model + `/` |
| `add-feature-slice` (new) | templates from `API_PROJECT_STRUCTURE.md`, as `references/` | model + `/` |
| `implement-ticket` | now creates `specs/<slug>/` and verifies ACs | `/` only |
| `analyze-requirement` | `analyze-po-requirement`; now writes the spec files | `/` only |
| `onboarding` | `onboard-analyzer` | `/` only |
| `update-docs` | `manage-documentation` | `/` only |
| `fix-warnings` | — | `/` only |

`analyze-codebase` moves into the `codebase-analyzer` agent. The other two agents become thin
wrappers that point at their skill's `SKILL.md` path.

### Rules

| Rule | `paths` | Built from |
|---|---|---|
| `backend.md` | `src/**/*.cs` | `CODING_CONVENTIONS.md` + architecture "Core Principles" |
| `backend-testing.md` | `tests/**/*.cs` | `rules/backend-testing.md` |
| `frontend.md` | `src/UI/mango-ui/src/**/*.ts`, `…/*.tsx` | `memory/domains/frontend-spa.md` facts, with MUI removed |
| `docs.md` | `docs/**/*.md` | `rules/documentation-standards.md` |
| `specs.md` | `specs/**/*.md` | the AC and status conventions from agentic-devkit |
| `memory/*.md` (4) | per domain | one-line pointers that replace the always-on memory index import |

## 6. Budgets and lint (`sync-agent-harness.ps1 -Check`, run in CI)

| Category | Enforces |
|---|---|
| Budget | `AGENTS.md` ≤ 4,096 B · `CLAUDE.md` ≤ 1,024 B · always-on ≤ 5,120 B |
| Rules | ≥ 1 glob in `paths`, no prose, ≤ 4,096 B |
| Skills | name = folder; description starts `Use when`, ≤ 300 chars; `SKILL.md` ≤ 6,144 B; ≤ 6 model-invocable; user-only skills referenced by path, never by bare name |
| Agents | name = file; description ≤ 300 chars; body ≤ 3,072 B |
| Links | backticked repo paths (harness files) and relative markdown links (harness + specs) resolve; markdown-link rules ported from agentic-devkit `check_kit.py` |
| Staleness | retired terms and paths absent from harness files |
| Drift | `.github/` mirror equals a fresh generation, ignoring line endings |
| Facts | backticked symbols and packages exist in the source corpus or are allowlisted with a reason (§9.1) |
| Specs | status, unique AC ids, AC ↔ test-plan traceability (§8) |
| Evals | every `evals.json` parses, names an existing skill, has unique ids and valid check types (§9.2) |

Expected result: always-on 12.7 KB -> ≤ 5 KB (about -60%), model-visible skill descriptions 15 -> 5,
duplicated rule text 13.6 KB -> 0, and three known factual errors caught by CI.

## 7. Decisions and out of scope

- No `.github/copilot-instructions.md`, because Copilot already reads `AGENTS.md`.
- No `.agents/skills/`, Codex or Antigravity targets.
- Keep the `.agent/` name for memory and tickets.
- Drop conceptual terms (`Repository pattern`, `Clean Architecture`) from the staleness lint, because guardrails name them legitimately.
- **Spec format only from agentic-devkit**, chosen by the user. The `/new-feature`, `/implement`,
  `/review-code` and `/ship` commands and `planning/` aren't ported: the ticket stays the progress
  tracker, and the spec is the contract it links to.
- **Evals live in `tests/skill-evals/`, not inside the skill folder.** A with-skill session could
  otherwise read `evals.json` and see the expected answers. It also keeps the skill folder exactly
  what both tools load.
- **Not built (user's choice):** skill trigger evals; AC-vs-code review and the
  Completeness/Correctness/Leverage report (agentic-devkit `analyze-fx-efficiency`).
- **Not changed:** the project-level `superpowers` plugin setting (adds per-session context; the user's call).

## 8. Specs (ported from agentic-devkit)

The `specs/_template/` files are adapted from agentic-devkit `specs/_template/`:

- `spec.md` keeps the devkit sections: Status / Owner / Last updated, Problem, User & outcome,
  Scope In/Out, Constraints & assumptions, Open questions, Related. The Related links point at
  `.agent/memory/index.md` and `docs/ARCHITECTURE.md`.
- `acceptance-criteria.md` uses `- [ ] AC-n: Given … when … then …`, a Non-functional section, and an Out-of-scope reminder.
- `test-plan.md` has a coverage table with MangoAspire's layers: `| AC | Unit (xUnit) | Integration | Lint/Build | Manual |`.
  There's no frontend test runner, so SPA ACs map to Lint/Build or Manual. The devkit's
  `tests/cases/` scenario file isn't ported; scenarios are listed inline.

Lifecycle: `/analyze-requirement` writes the three files (Status `draft` → `in-review`); approval
sets `approved`; `/implement-ticket` Step 3 checks each AC with cited evidence; completion sets
`shipped`. The Specs lint enforces the status vocabulary, unique AC ids, and that the test plan
maps exactly the defined ACs.

## 9. Evaluating AI-generated content

### 9.1 Layer 1 — fact-check (deterministic, free, in CI)

Harness text is AI-written and goes stale silently. The `Facts` lint extracts backticked tokens from
harness files, skipping fenced code blocks and `specs/`, because specs describe code that doesn't
exist yet. A token is a candidate when it is identifier-shaped (`Name`, `Name.Member`, `Name<T>`,
`Name()`, `useX`, `@scope/pkg`) and contains an uppercase letter or starts with `use` or `@`. File
names (a last segment of ≤ 5 lowercase chars, such as `.md` or `.json`), all-caps acronyms, and
tokens under 4 chars are skipped. Each candidate must occur, case-insensitively, in the **source
corpus**:

- `*.cs`, `*.csproj`, `*.cshtml`, `*.razor`, `*.ts`, `*.tsx`, `*.json`, `*.props` and `*.yml` under `src/` and `tests/`
- root `*.sln`, `*.props` and `docker-compose*.yml`

`bin/`, `obj/`, `node_modules/`, `dist/` and `tests/skill-evals/` are excluded. A candidate that
isn't found must be listed, with a reason, in `scripts/harness/facts-allowlist.txt`. Prose claims
without backticks (like the bolded MUI line) are caught only by the staleness list or by output
evals. This layer catches renamed or hallucinated APIs, packages and projects.

### 9.2 Layer 2 — skill output evals (on demand, costs tokens)

This is the agentic-devkit `skill-creator` method, adapted to a scripted runner:

- **Definition.** `tests/skill-evals/<skill>/evals.json` follows the devkit schema (`skill_name`,
  `evals[].id/prompt/expected_output/expectations`) and adds `name`, `setup` (commands run in the
  worktree first, e.g. `git apply` of a fixture patch) and `checks`. A check is deterministic:
  `command`, `file-matches`, `no-edits`, `no-commit` or `response-matches`.
- **Run.** For each eval, the runner makes two runs, `with_skill` (prompt `/<skill> <prompt>`) and
  `without_skill` (the skill folder is deleted from the worktree and the prompt is plain). Each runs
  `claude -p --output-format json` in a disposable `git worktree` of `HEAD`, and the runner saves
  the response, the diff and cost/duration.
- **Grade.** Deterministic checks run first. Then a grader `claude -p` call judges the
  `expectations` from the response, diff and check results, using a prompt adapted from devkit
  `skill-creator/agents/grader.md` (Apache-2.0). It writes `grading.json` with
  `expectations[{text, passed, evidence}]` (skill-creator schema).
- **Report.** `benchmark.json` and `summary.md` give, per skill: with/without pass rates, Δ, a
  verdict (`helps` if Δ ≥ +0.10, `hurts` if Δ ≤ −0.10, otherwise `neutral`), cost, duration, and
  non-discriminating expectations (ones that pass in both configs).
- **Scope of the first run:** `code-review`, `create-unit-test`, `run-tests`, `generate-commit`,
  `add-feature-slice` and `analyze-requirement`, with 3 evals each: 36 sessions plus up to 36
  grader calls.
