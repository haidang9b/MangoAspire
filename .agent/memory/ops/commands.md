# Command Reference

The dev machine is **Windows 11 + PowerShell**. Helper scripts in `scripts/` are PowerShell; see
`.agent/tools/scripts.md` for the full catalogue.

## Dev scripts (`scripts/`)

- `pwsh ./scripts/bootstrap.ps1` — one-shot onboarding (restore, pnpm install, sync harness,
  render the ticket board, start MCP).
- `pwsh ./scripts/run-app.ps1` — launch the Aspire AppHost.
- `pwsh ./scripts/build.ps1` · `test.ps1` · `format.ps1` · `ui.ps1` · `start-mcp.ps1`.
- `pwsh ./scripts/sync-agent-harness.ps1` (`-Check` for a dry run) — regenerate `.claude/` from
  `.agent/`.
- `pwsh ./scripts/update-ticket-board.ps1` (`-Check`, `-Open`) — regenerate `.agent/ui/board.html`
  from the ticket JSONs.

## Backend (.NET)

- Restore: `dotnet restore MangoAspire.sln`
- Build all: `dotnet build MangoAspire.sln` · one project:
  `dotnet build src/Services/Products.API/Products.API.csproj`
- Run the Aspire host: `dotnet run --project src/Mango.AppHost/Mango.AppHost.csproj`
- Format: `dotnet format MangoAspire.sln` · verify:
  `dotnet format --verify-no-changes MangoAspire.sln`
- Test all: `dotnet test MangoAspire.sln`
- One project: `dotnet test tests/Services/Products.API.Tests/Products.API.Tests.csproj`
- One method: `dotnet test <proj> --filter "FullyQualifiedName~ClassName.MethodName"`

## Frontend (pnpm, from the repo root — note `--dir`)

- Install: `pnpm install --dir src/UI/mango-ui`
- Dev: `pnpm --dir src/UI/mango-ui dev`
- Build: `pnpm --dir src/UI/mango-ui build`
- Lint: `pnpm --dir src/UI/mango-ui lint`
