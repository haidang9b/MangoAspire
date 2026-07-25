# CLAUDE.md

MangoAspire is a .NET 10 + Aspire microservices e-commerce platform (dual identity: Duende/OpenIddict; MVC + React/Vite SPA; PostgreSQL, RabbitMQ, Debezium).

This file is a thin discovery shim for Claude Code. The authoritative build/lint/test/architecture
guidance lives in `AGENTS.md` and is imported below — keep it single-sourced there, not duplicated here.

@AGENTS.md

## Additional conventions

- @.agent/CODING_CONVENTIONS.md
- @.agent/API_PROJECT_STRUCTURE.md
- Rule set: `.agent/rules/` (architecture, backend-dotnet, backend-testing, frontend-react, documentation-standards).

## Guardrails (these override any stale phrasing you may encounter)

- **Vertical Slice Architecture** — organize by feature slice. NOT Clean/Onion/N-tier layers.
- **No Repository pattern** — use `DbContext` directly in handlers.
- **pnpm**, not npm, for the SPA: `pnpm --dir src/UI/mango-ui <dev|build|lint>`.
- **No frontend test runner exists.** Verify the SPA with `pnpm --dir src/UI/mango-ui lint` + `build`. Do not invent test commands.

## Harness map

Claude Code's native harness is **generated from `.agent/`** (the source of truth):

- Skills → `.claude/skills/`  · Subagents → `.claude/agents/`  · Slash commands → `.claude/commands/` (each `@`-imports its `.agent/workflows/*.md`).
- Regenerate after editing `.agent/`: `pwsh ./scripts/sync-agent-harness.ps1` (`-Check` for a CI/pre-commit dry-run). Do not hand-edit generated files — they carry a "GENERATED" banner.
- **Serena MCP** (semantic C# code navigation) is wired via `.mcp.json` → `http://localhost:9121`. Start it first: `pwsh ./scripts/start-mcp.ps1`, then confirm with `/mcp`.

## Dev scripts (PowerShell, in `scripts/`)

- `bootstrap.ps1` — one-shot onboarding (restore, pnpm install, sync harness, start MCP).
- `run-app.ps1` — launch the Aspire AppHost · `build.ps1` · `test.ps1` · `format.ps1` · `ui.ps1` · `start-mcp.ps1`.
