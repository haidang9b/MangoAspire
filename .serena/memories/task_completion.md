# Task Completion Checklist

Run the smallest valid command set for the changed area (do NOT modify unrelated files, do NOT introduce new architecture patterns without explicit request).

## Backend change
1. `dotnet build MangoAspire.sln` (or the affected project `.csproj`).
2. Affected tests: `dotnet test tests/Services/<Area>.API.Tests/<Area>.API.Tests.csproj` (optionally `--filter "FullyQualifiedName~..."`).
3. Formatting: `dotnet format --verify-no-changes MangoAspire.sln` (run `dotnet format` to fix).

## Frontend change (`src/UI/mango-ui`)
1. `pnpm --dir src/UI/mango-ui lint`
2. `pnpm --dir src/UI/mango-ui build`
- No frontend test runner exists — do NOT invent test commands.

## After editing `.agent/` harness sources
- Regenerate: `pwsh ./scripts/sync-agent-harness.ps1` (CI check: `-Check`).

Relevant `.agent/` skills exist for these flows (`run-tests`, `fix-warnings`, `review`, `manage-documentation`).
