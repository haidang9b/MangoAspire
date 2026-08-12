# Serena MCP — semantic C# navigation

Serena provides symbol-level navigation over the C# codebase (find symbol, find references, inspect
a class without reading whole files). It is wired via `.mcp.json` at the repo root:

```json
{ "mcpServers": { "serena": { "type": "http", "url": "http://localhost:9121/mcp" } } }
```

`.claude/settings.json` enables it through `"enabledMcpjsonServers": ["serena"]`.

## Lifecycle

- Start: `pwsh ./scripts/start-mcp.ps1` · Stop: `pwsh ./scripts/start-mcp.ps1 -Down`
- Runs via `docker-compose.mcp.yaml`, reachable at `http://localhost:9121`.
- **The container must be up before launching Claude Code** — `.mcp.json` is read at startup. If
  `/mcp` shows it disconnected, reconnect from there; no restart is needed.

## When it beats grep

Use Serena for "who calls this", "where is this interface implemented", and "show me this symbol's
definition" across a solution of this size. Plain `Grep`/`Glob` remain better for text patterns,
config files and anything outside C#.

## Do not use Serena's memory tools

Project memory lives in `.agent/memory/` — see `.agent/memory/index.md`. The old
`.serena/memories/` store was folded into it and deleted, and `.serena/memories/` is gitignored so
it cannot silently come back. Do **not** run Serena's onboarding or `write_memory` tools: anything
they write is invisible to the rest of the harness and will drift.
