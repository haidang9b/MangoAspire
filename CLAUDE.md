# CLAUDE.md

MangoAspire is a .NET 10 + Aspire microservices e-commerce platform (dual identity: Duende/OpenIddict; MVC + React/Vite SPA; PostgreSQL, RabbitMQ, Debezium).

This file is a thin discovery shim for Claude Code. The authoritative build/lint/test/architecture
guidance lives in `AGENTS.md` and is imported below — keep it single-sourced there, not duplicated here.

@AGENTS.md

## Additional conventions

Read on demand — plain paths, not `@`-imports (AGENTS.md above already carries the core rules):

- `.agent/CODING_CONVENTIONS.md` — before writing backend C# (Program.cs, validation, error handling, data access, routes).
- `.agent/API_PROJECT_STRUCTURE.md` — before adding an API project, feature slice, route group, or integration event.
- Rule set: `.agent/rules/` (architecture, backend-dotnet, backend-testing, frontend-react, documentation-standards).

## Guardrails (these override any stale phrasing you may encounter)

- **Vertical Slice Architecture** — organize by feature slice. NOT Clean/Onion/N-tier layers.
- **No Repository pattern** — use `DbContext` directly in handlers.
- **No cross-slice calls** — a feature never uses another feature's handlers or logic; share only infrastructure.
- **pnpm**, not npm, for the SPA: `pnpm --dir src/UI/mango-ui <dev|build|lint>`.
- **No frontend test runner exists.** Verify the SPA with `pnpm --dir src/UI/mango-ui lint` + `build`. Do not invent test commands.

## Project memory & tickets

@.agent/memory/index.md

- The line above imports the **index only** — it links out with plain paths, and you read the domain
  files it names on demand. Do not convert those links into `@`-imports.

## Harness map

Claude Code's native harness is **generated from `.agent/`** (the source of truth):

- Skills → `.claude/skills/`  · Subagents → `.claude/agents/`  · Slash commands → `.claude/commands/` (each `@`-imports its `.agent/workflows/*.md`).
- Regenerate after editing `.agent/`: `pwsh ./scripts/sync-agent-harness.ps1` (`-Check` for a CI/pre-commit dry-run). Do not hand-edit generated files — they carry a "GENERATED" banner.
- **Serena MCP** (semantic C# code navigation) is wired via `.mcp.json` → `http://localhost:9121`. Start it first: `pwsh ./scripts/start-mcp.ps1`, then confirm with `/mcp`. Do **not** use Serena's memory tools — project memory lives in `.agent/memory/`.
- Full layout and ownership map: `.agent/README.md`.

## Dev scripts (PowerShell, in `scripts/`)

- `bootstrap.ps1` — one-shot onboarding (restore, pnpm install, sync harness, start MCP).
- `run-app.ps1` — launch the Aspire AppHost · `build.ps1` · `test.ps1` · `format.ps1` · `ui.ps1` · `start-mcp.ps1`.
