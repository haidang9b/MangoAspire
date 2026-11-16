# `.agent/` — project knowledge and work state

The agent harness is split by what each tool can read natively. This folder holds only what no tool
loads by itself: **memory**, **tickets** and **tooling notes**. Everything here is version-controlled
and shared; nothing here is machine-local.

## Where everything lives

| Location | Holds | Loaded | Written by |
|---|---|---|---|
| `AGENTS.md` | Always-on cross-tool index: commands, guardrails, pointers (≤ 4 KB) | every session (Claude via `CLAUDE.md`, Copilot directly) | humans |
| `CLAUDE.md` | `@AGENTS.md` + Claude-only notes (≤ 1 KB) | every Claude session | humans |
| `.claude/rules/` | **Normative**, path-scoped conventions (`paths:`) | when a matching file is read or edited | humans |
| `.claude/skills/` | Skills: 5 model-invocable, 5 user-only workflows | description at start; body on use | humans |
| `.claude/agents/` | Subagents (thin, point at their skill) | when spawned | humans |
| `.github/instructions/`, `.github/agents/` | **Generated** Copilot mirror of rules and agents | by Copilot | `scripts/sync-agent-harness.ps1` |
| `specs/<slug>/` | Feature contract: spec, acceptance criteria, test plan (agentic-devkit format) | on demand | agent + humans |
| `.agent/memory/` | **Descriptive**: what is true, what we learned | on demand (index + pointer rules) | agent, at ticket completion |
| `.agent/tickets/` | One `ticket.md` per ticket: status, steps, notes | on demand | agent, continuously |
| `.agent/state/` | Tiny derived cache: which ticket is active | on demand | agent |
| `.agent/tools/` | Tooling notes: harness, skill evals, Serena MCP, dev scripts | on demand | humans |
| `tests/skill-evals/` | Skill output evals (`evals.json` + fixtures) | never by sessions under test | humans |

`docs/` is **human documentation**. It holds no agent memory, ticket state or specs.

## Normative vs descriptive

`.claude/rules/backend.md` says *"every command has a FluentValidation validator"*. Memory says
*"`Mango.Orchestrators` keeps a SagaRepository, a deliberate exception to the no-repository rule"*.
Keeping these apart is what stops one convention being restated in three files and drifting.

## Keeping it honest

`scripts/sync-agent-harness.ps1 -Check` runs in CI. It checks:
- size budgets;
- rule and skill shape;
- broken paths and links;
- stale terms;
- drift of the Copilot mirror;
- **facts**: backticked symbols must exist in the code;
- **specs**: AC ↔ test-plan traceability;
- eval-set validity.

See `.agent/tools/harness.md`. Skill quality is measured separately with
`scripts/eval-skills.ps1` (see `.agent/tools/skill-evals.md`).

## Working a ticket

Everything for a ticket is one file, `.agent/tickets/<TICKET-ID>/ticket.md`, plus an optional
`artifacts/` folder.

- **Front-matter** (`kind`, `status`, `priority`, timestamps) is the header.
- **`## Steps`** checkboxes are progress. A step is closed once every task under it is checked,
  either done or struck through as `— skipped: <reason>`.
- **`## Plan`, `## Decisions`, `## Gotchas`, `## Open Questions`, `## Blockers` and `## Session Log`**
  are prose.
- **`kind`** (`feature | bug | chore | security | refactor | spike`) selects the template under
  `.agent/tickets/_templates/`. Feature tickets link a spec in `specs/<slug>/`.

Completed tickets stay where they are; `status: completed` *is* the archive.

## Loading memory without burning context

Nothing imports `.agent/memory/index.md`. `AGENTS.md` links it, and the pointer rules in
`.claude/rules/memory/` surface the right domain file when you touch that area's code.

> Link memory files with plain backticked paths, never `@`-imports. Claude Code expands `@`
> recursively, and one `@` would pull the memory tree into every session.

See `.agent/memory/MEMORY_GUIDE.md` for what belongs in memory and what does not.
