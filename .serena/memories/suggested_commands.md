# Suggested Commands

Dev machine is **Windows 11 + PowerShell** (the Serena container reports Linux, but the user runs PowerShell). Helper scripts live in `scripts/` (PowerShell).

## Dev scripts (`scripts/`, PowerShell)
- `pwsh ./scripts/bootstrap.ps1` — one-shot onboarding (restore, pnpm install, sync harness, start MCP).
- `pwsh ./scripts/run-app.ps1` — launch the Aspire AppHost.
- `pwsh ./scripts/build.ps1` · `test.ps1` · `format.ps1` · `ui.ps1` · `start-mcp.ps1`.
- `pwsh ./scripts/sync-agent-harness.ps1` (`-Check` for CI dry-run) — regenerate `.claude/` from `.agent/`.

## Backend (.NET)
- Restore: `dotnet restore MangoAspire.sln`
- Build all: `dotnet build MangoAspire.sln` · one project: `dotnet build src/Services/Products.API/Products.API.csproj`
- Run Aspire host: `dotnet run --project src/Mango.AppHost/Mango.AppHost.csproj`
- Format: `dotnet format MangoAspire.sln` · verify: `dotnet format --verify-no-changes MangoAspire.sln`
- Test all: `dotnet test MangoAspire.sln`
- Test one project: `dotnet test tests/Services/Products.API.Tests/Products.API.Tests.csproj`
- Test one method: `dotnet test <proj> --filter "FullyQualifiedName~ClassName.MethodName"`

## Frontend (pnpm, from repo root — note `--dir`)
- Install: `pnpm install --dir src/UI/mango-ui`
- Dev: `pnpm --dir src/UI/mango-ui dev` · Build: `pnpm --dir src/UI/mango-ui build` · Lint: `pnpm --dir src/UI/mango-ui lint`

## Serena MCP server (semantic C# nav)
- Start: `pwsh ./scripts/start-mcp.ps1` · Stop: `pwsh ./scripts/start-mcp.ps1 -Down`
- Runs via `docker-compose.mcp.yaml`, reachable at `http://localhost:9121`. Container must be up BEFORE launching Claude Code (it reads `.mcp.json` at startup). If `/mcp` shows disconnected, reconnect there — no restart needed.
