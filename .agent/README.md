# `.agent/` — the agent harness

Single source of truth for how coding agents work in this repository. Everything here is
version-controlled and shared; nothing here is machine-local.

## What each directory owns

| Directory | Holds | Written by |
|---|---|---|
| `rules/` | **Normative** — what you *must* do, triggered by file globs | humans |
| `CODING_CONVENTIONS.md`, `API_PROJECT_STRUCTURE.md` | Normative, imported by `CLAUDE.md` | humans |
| `memory/` | **Descriptive** — what is *true* about this project and what we learned | agent, at ticket completion |
| `tickets/` | One `ticket.md` per ticket: status, plan and notes in a single file | agent, continuously |
| `state/` | Tiny derived cache — which ticket is active | agent |
| `tools/` | Tooling and integrations: MCP servers, dev scripts, harness generation | humans |
| `skills/`, `agents/`, `workflows/` | Claude Code harness sources — the only dirs the sync script reads | humans |

`docs/` is **human documentation**. It holds no agent memory and no ticket state.

## Normative vs descriptive

`rules/` says *"use FluentValidation for every command"*. `memory/` says *"`Mango.Orchestrators`
keeps a SagaRepository — a deliberate exception to the no-repository rule"*. Keeping these apart is
what stops the same convention being restated in three files and drifting.

## Generated files — never hand-edit

| Generated | From | By |
|---|---|---|
| `.claude/{skills,agents,commands}/` | `.agent/{skills,agents,workflows}/` | `pwsh ./scripts/sync-agent-harness.ps1` (`-Check` for drift) |

Carries a "GENERATED — do not edit directly" banner. Edit the source, then re-run the script.

## Working a ticket

Everything for a ticket is one file: `.agent/tickets/<TICKET-ID>/ticket.md` (plus an optional
`artifacts/` folder for extras like code reviews or walkthroughs).

- **Front-matter** (`kind`, `status`, `priority`, timestamps) is the header.
- **`## Steps`** checkboxes are progress. A step is closed once every task under it is checked
  (done, or struck through as `— skipped: <reason>`).
- **`## Plan`, `## Decisions`, `## Gotchas`, `## Open Questions`, `## Blockers`, `## Session Log`**
  are prose.

There's nothing to keep in sync across files, because there's only one file. `kind` —
`feature | bug | chore | security | refactor | spike` — selects which template under
`.agent/tickets/_templates/` a new ticket starts from; each kind has a lighter or heavier `## Plan`
shape to match the work (a `chore` doesn't get feature-style BA/PO ceremony).

Completed tickets stay where they are; `status: completed` *is* the archive. Nothing gets moved.

## Loading memory without burning context

`CLAUDE.md` imports **only** `.agent/memory/index.md` (~1.5 KB). That index says *when* to read each
domain file; the agent reads those on demand.

> **`index.md` must link with plain backticked paths, never `@`-imports.** Claude Code follows `@`
> recursively — one `@` in the index pulls the entire memory tree into every session and defeats the
> whole design.

See `.agent/memory/MEMORY_GUIDE.md` for what belongs in memory and what does not.
