# Agent harness — layout, Copilot mirror and lint

`.claude/` is canonical. Claude Code reads `.claude/{rules,skills,agents}`; VS Code Copilot reads
`.claude/skills` natively. `scripts/sync-agent-harness.ps1` generates only what Copilot can't read
from `.claude/`:

| Source (canonical) | Generated mirror | Transformation |
|---|---|---|
| `.claude/rules/**/*.md` | `.github/instructions/**/*.instructions.md` | `paths:` → `applyTo: "<globs joined by ,>"`; body copied |
| `.claude/agents/*.md` | `.github/agents/*.agent.md` | frontmatter reduced to `name` + `description`; body copied |

Generated files carry a `<!-- GENERATED from .claude/ ... -->` banner. Never hand-edit them: edit the
source and re-run the script. Write mode deletes and regenerates **only** `.github/instructions/` and
`.github/agents/`; `.github/workflows/` is never touched.

## Commands

- Sync: `pwsh ./scripts/sync-agent-harness.ps1` (Windows PowerShell 5.1: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1`).
  It prints a context report:

  ```text
  AlwaysOnBytes: 3548  ModelSkillCount: 5  UserSkillCount: 5  SkillListingChars: 758
  ```

- Lint: add `-Check`, optionally with `-Category A,B`. It exits 1 on any finding. CI runs it.
- Tests: `Invoke-Pester -Path tests/harness` (Pester 5+).

## Lint categories

| Category | Enforces |
|---|---|
| Budget | `AGENTS.md` ≤ 4,096 B · `CLAUDE.md` ≤ 1,024 B · always-on (`CLAUDE.md` + `@`-imports + rules without `paths`) ≤ 5,120 B |
| Rules | ≥ 1 glob in `paths`, no prose, ≤ 4,096 B. A rule without valid `paths` loads in every session |
| Skills | name = folder; description starts `Use when`, ≤ 300 chars; `SKILL.md` ≤ 6,144 B; ≤ 6 model-invocable; user-only skills referenced by path, never by bare name |
| Agents | name = file; description ≤ 300 chars; body ≤ 3,072 B |
| Links | backticked repo paths resolve (harness files); relative markdown links resolve (harness files + `specs/`) |
| Staleness | retired terms and paths, matched case-sensitively on word boundaries |
| Drift | the `.github/` mirror equals a fresh generation, ignoring line endings |
| Facts | backticked symbols and packages exist in the source corpus, or are in `scripts/harness/facts-allowlist.txt` with a reason |
| Specs | each `specs/<slug>/` has the 3 files, a valid `**Status:**`, unique `AC-n`, and a test plan mapping exactly those ACs |
| Evals | each `tests/skill-evals/<skill>/evals.json` parses, names an existing skill, and uses known check types |

The **harness files** scanned are `AGENTS.md`, `CLAUDE.md`, `.claude/**/*.md`, `.agent/README.md`,
`.agent/tools/*.md` and `.agent/memory/**/*.md`. Specs are excluded from Facts and Staleness
because they describe code that doesn't exist yet.

## Facts lint

AI-written guidance goes stale silently: a rule once mandated a UI library the SPA doesn't use, and
memory described a project that doesn't exist. The Facts lint takes identifier-shaped backticked tokens (shapes like Name, Name.Member, Name<T>,
Name(), useX and @scope/pkg) and requires each to appear in the code. It skips:
- fenced blocks;
- file names;
- acronyms;
- lowercase words.

When a name is legitimately absent, such as an anti-pattern being warned against or an illustrative
example, allowlist it with `Token  # reason`. Prose claims without backticks are not checked.

## Code

- `scripts/harness/AgentHarness.psm1` — mirror, drift, lint, context report.
- `scripts/harness/SkillEval.psm1` — skill output evals (see `.agent/tools/skill-evals.md`).
- `tests/harness/*.Tests.ps1` — Pester tests on fixture repos.

Everything must run on Windows PowerShell 5.1 and PowerShell 7. Keep scripts ASCII-only (5.1
reads BOM-less files as ANSI), and avoid `??`, `?.` and the ternary operator.
