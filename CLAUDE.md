# CLAUDE.md

@AGENTS.md

## Claude Code specifics

- Rules in `.claude/rules/` load automatically when you read or edit matching files; don't pre-read them.
- Serena MCP (`.mcp.json`, port 9121): start with `./scripts/start-mcp.ps1`, confirm with `/mcp`. Never use Serena's memory tools; memory lives in `.agent/memory/`.
- After editing `.claude/rules/` or `.claude/agents/`, run `./scripts/sync-agent-harness.ps1` to refresh the Copilot mirror in `.github/`.
