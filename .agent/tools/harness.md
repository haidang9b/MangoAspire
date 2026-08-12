# Harness generation — `.agent/` → `.claude/`

`.agent/` is the source of truth. `scripts/sync-agent-harness.ps1` converts it into the layout
Claude Code discovers natively.

| Source | Destination | Transformation |
|---|---|---|
| `.agent/skills/<x>/SKILL.md` | `.claude/skills/<x>/SKILL.md` | frontmatter `name` is forced to the directory name; the whole skill directory is copied recursively |
| `.agent/agents/<x>.agent.md` | `.claude/agents/<x>.md` | non-Claude `tools:` dropped; `argument-hint:` demoted to a `> Usage:` line in the body |
| `.agent/workflows/<x>.md` | `.claude/commands/<x>.md` | a stub carrying the workflow's `description` and `@`-importing the source doc |

Everything else in `.agent/` — `rules/`, `memory/`, `tickets/`, `state/`, `tools/`, `schemas/`,
`ui/` — is **never** synced. Those reach the agent through `CLAUDE.md` imports or on-demand reads.

## Rules

- Generated files carry a `<!-- GENERATED ... do not edit directly -->` banner. Edit the source in
  `.agent/`, then re-run the script.
- Write mode **deletes and regenerates** `.claude/{skills,agents,commands}`, so removals propagate
  and hand edits are destroyed. `.claude/settings.json` is hand-authored and left alone.
- `-Check` regenerates into a temp directory and compares SHA256 tree signatures, exiting non-zero
  on drift. Use it before committing.
- Mechanical path fixups are applied automatically (`$SafeReplacements`). Conceptual staleness
  (`npm run`, `Vitest`, `Repository pattern`, the retired `docs/tracking` paths…) is **warned about,
  never auto-edited** — fix it at the source and re-sync.

## Related generated artifact

`.agent/ui/board.html` is generated from the ticket JSONs by `scripts/update-ticket-board.ps1`. It
is a separate pipeline: the two scripts do not touch each other's inputs or outputs.
