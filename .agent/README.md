# `.agent/` — the agent harness

Single source of truth for how coding agents work in this repository. Everything here is
version-controlled and shared; nothing here is machine-local.

## What each directory owns

| Directory | Holds | Written by |
|---|---|---|
| `rules/` | **Normative** — what you *must* do, triggered by file globs | humans |
| `CODING_CONVENTIONS.md`, `API_PROJECT_STRUCTURE.md` | Normative, imported by `CLAUDE.md` | humans |
| `memory/` | **Descriptive** — what is *true* about this project and what we learned | agent, at ticket completion |
| `tickets/` | Per-ticket state (`ticket.json`) + prose memory (`notes.md`) + blueprint (`plan.md`) | agent, continuously |
| `state/` | Tiny derived cache — which ticket is active | agent |
| `tools/` | Tooling and integrations: MCP servers, dev scripts, harness generation | humans |
| `schemas/` | JSON Schema for `ticket.json` | humans |
| `ui/` | `board.template.html` (source) → `board.html` (**generated**) | script |
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
| `.agent/ui/board.html` | `.agent/tickets/*/ticket.json` | `pwsh ./scripts/update-ticket-board.ps1` (`-Check` for drift) |

Both carry a "GENERATED — do not edit directly" banner. Edit the source, then re-run the script.

## Working a ticket

Everything lives in `.agent/tickets/<TICKET-ID>/`:

- `ticket.json` — **canonical state**: status, ordered steps/tasks with `done` flags, blockers, links.
- `notes.md` — **prose memory**: decisions, gotchas, open questions, blocker detail, session log.
- `plan.md` — the approved technical blueprint.
- `artifacts/` — optional extras (code reviews, walkthroughs).

Two rules keep them from contradicting each other:

1. **JSON owns state.** Status and progress are read from `ticket.json` only.
2. **`notes.md` cannot claim state** — no `- [ ]` checkboxes, no "Current Status:" lines. It has no
   syntax capable of asserting progress, so it can never disagree.

Completed tickets stay where they are; `status: "completed"` *is* the archive. Nothing gets moved.

## Loading memory without burning context

`CLAUDE.md` imports **only** `.agent/memory/index.md` (~1.5 KB). That index says *when* to read each
domain file; the agent reads those on demand.

> **`index.md` must link with plain backticked paths, never `@`-imports.** Claude Code follows `@`
> recursively — one `@` in the index pulls the entire memory tree into every session and defeats the
> whole design.

See `.agent/memory/MEMORY_GUIDE.md` for what belongs in memory and what does not.
