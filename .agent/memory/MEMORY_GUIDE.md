# Memory Guide

How to write and prune `.agent/memory/`. Read this before adding a memory file.

## Discovery model

- Progressive discovery through references: a small index links out, and files are read on demand.
- `.agent/memory/index.md` is the graph root and the **only** memory file `CLAUDE.md` loads.
- The **referrer** states when to read a file, not the file itself. A line in `index.md` should say
  which aspects it covers — "Razor views, Emerald Calm CSS" beats "UI stuff".
- References are plain backticked repo-relative paths: `` `.agent/memory/domains/identity.md` ``.

> **Never use `@`-imports inside memory files.** Claude Code resolves `@` recursively, so a single
> `@` in `index.md` loads the whole tree every session — which is exactly what this structure exists
> to avoid.

## Style

Dense agent notes, not prose docs. Prefer invariants and terse bullets. Skip obvious context,
rationale and examples unless they prevent a likely mistake. Keep guidance durable and
generalizable, not task-local.

## Normative vs descriptive

Memory is **descriptive** — what is true, what we learned, what surprised us. Rules you must follow
belong in `.agent/rules/`, `AGENTS.md`, or `.agent/CODING_CONVENTIONS.md`. If a note reads like
"always do X", it is a rule and belongs there instead. The exception worth recording in memory is
the *deviation*: "service Y breaks rule X because Z".

## Add/update threshold

Add or update memory only for stable, non-obvious project facts that would otherwise cost a
rediscovery. Do **not** add:

- quick-read facts (anything one grep answers)
- generic language or framework knowledge
- one-off task notes — those live in the ticket's `notes.md`
- volatile line-level details (line numbers, exact method bodies)
- behaviour that is about to change

## Splitting

One file per bounded area, ideally under ~150 lines. When a domain file outgrows that, split it and
update `index.md` in the same edit. Never append to a catch-all — an ever-growing
"project-context" file is what this structure replaced.

## Maintenance

- Renaming or moving a memory file: grep the repo for the old path and fix every referrer
  (`index.md` at minimum, plus any `links.memory[]` in `.agent/tickets/*/ticket.json`).
- Every ticket records the memory files it fed in `links.memory[]`, so any line can be traced back
  to the ticket that produced it.
- Deleting: prefer deleting a stale memory to leaving it. A wrong memory costs more than a missing
  one.
