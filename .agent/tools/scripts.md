# Dev scripts (`scripts/`)

All PowerShell and runnable from anywhere: each resolves the repo root from `$PSScriptRoot`. The
harness scripts share `scripts/harness/*.psm1`; the rest are standalone. Windows PowerShell 5.1 runs
them as `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/<x>.ps1`.

| Script | Purpose |
|---|---|
| `bootstrap.ps1` | One-shot onboarding: prerequisite check, `dotnet restore`, `pnpm install`, sync the harness, start the Serena MCP container. `-SkipMcp`, `-SkipRestore`. |
| `run-app.ps1` | Launch the Aspire AppHost. |
| `build.ps1` | Build the solution. |
| `test.ps1` | Run the xUnit suite. |
| `format.ps1` | `dotnet format` over the solution. |
| `ui.ps1` | Frontend helper (pnpm dev/build/lint in `src/UI/mango-ui`). |
| `start-mcp.ps1` | Start/stop the Serena MCP container (`-Down`). See `.agent/tools/mcp-serena.md`. |
| `sync-agent-harness.ps1` | Sync the Copilot mirror (`.github/instructions`, `.github/agents`) from `.claude/`; `-Check` lints the harness. See `.agent/tools/harness.md`. |
| `eval-skills.ps1` | Skill output evals, with vs without the skill (`-DryRun` first; costs tokens). See `.agent/tools/skill-evals.md`. |

## The generator script

`sync-agent-harness.ps1` follows a generator contract:
- the source of truth is `.claude/`;
- the generated artifact carries a "do not edit" banner;
- `-Check` regenerates into a temp location and compares text ignoring line endings, exiting non-zero
  on drift or on any lint finding.

Ticket state (`.agent/tickets/<ID>/ticket.md`) is **not** generated — it's a plain markdown file an
agent edits directly, with no separate build step and nothing to keep in sync.
