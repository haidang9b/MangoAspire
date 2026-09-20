# Dev scripts (`scripts/`)

All PowerShell, all standalone (no shared module by design), all runnable from anywhere — each
resolves the repo root from `$PSScriptRoot`.

| Script | Purpose |
|---|---|
| `bootstrap.ps1` | One-shot onboarding: prerequisite check, `dotnet restore`, `pnpm install`, sync the harness, start the Serena MCP container. `-SkipMcp`, `-SkipRestore`. |
| `run-app.ps1` | Launch the Aspire AppHost. |
| `build.ps1` | Build the solution. |
| `test.ps1` | Run the xUnit suite. |
| `format.ps1` | `dotnet format` over the solution. |
| `ui.ps1` | Frontend helper (pnpm dev/build/lint in `src/UI/mango-ui`). |
| `start-mcp.ps1` | Start/stop the Serena MCP container (`-Down`). See `.agent/tools/mcp-serena.md`. |
| `sync-agent-harness.ps1` | Generate `.claude/` from `.agent/` (`-Check`). See `.agent/tools/harness.md`. |

## The generator script

`sync-agent-harness.ps1` follows a generator contract: a source of truth (`.agent/`), a generated
artifact carrying a "do not edit" banner, and a `-Check` mode that regenerates into a temp location
and compares SHA256 hashes, exiting non-zero on drift.

Ticket state (`.agent/tickets/<ID>/ticket.md`) is **not** generated — it's a plain markdown file an
agent edits directly, with no separate build step and nothing to keep in sync.
