# Dev scripts (`scripts/`)

All PowerShell, all standalone (no shared module by design), all runnable from anywhere — each
resolves the repo root from `$PSScriptRoot`.

| Script | Purpose |
|---|---|
| `bootstrap.ps1` | One-shot onboarding: prerequisite check, `dotnet restore`, `pnpm install`, sync the harness, render the ticket board, start the Serena MCP container. `-SkipMcp`, `-SkipRestore`. |
| `run-app.ps1` | Launch the Aspire AppHost. |
| `build.ps1` | Build the solution. |
| `test.ps1` | Run the xUnit suite. |
| `format.ps1` | `dotnet format` over the solution. |
| `ui.ps1` | Frontend helper (pnpm dev/build/lint in `src/UI/mango-ui`). |
| `start-mcp.ps1` | Start/stop the Serena MCP container (`-Down`). See `.agent/tools/mcp-serena.md`. |
| `sync-agent-harness.ps1` | Generate `.claude/` from `.agent/` (`-Check`). See `.agent/tools/harness.md`. |
| `update-ticket-board.ps1` | Generate `.agent/ui/board.html` from the ticket JSONs (`-Check`, `-Open`). |

## The two generator scripts

Both follow the same contract: a source of truth, a generated artifact carrying a
"do not edit" banner, and a `-Check` mode that regenerates into a temp location and compares SHA256
hashes, exiting non-zero on drift.

`update-ticket-board.ps1` additionally **validates** ticket data. Structural problems (unknown
status, an id that disagrees with its directory name, a step whose `done` flag contradicts its
tasks) fail the run in both modes. Softer issues — a missing blueprint, checkboxes in `notes.md`, a
status/blocker mismatch — are warnings.

Its output must stay **deterministic**: no render timestamp, no random ids, fixed sort order.
Anything varying per run would make `-Check` fail every time.
