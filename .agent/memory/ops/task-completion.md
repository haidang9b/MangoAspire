# Task Completion Checklist

Run the smallest valid command set for the area you changed. Do not modify unrelated files and do
not introduce new architecture patterns without an explicit request.

## Backend change

1. `dotnet build MangoAspire.sln` (or just the affected `.csproj`).
2. Affected tests: `dotnet test tests/Services/<Area>.API.Tests/<Area>.API.Tests.csproj`
   (optionally `--filter "FullyQualifiedName~..."`).
3. Formatting: `dotnet format --verify-no-changes MangoAspire.sln` — run `dotnet format` to fix.

`WarningsAsErrors` is on in `Directory.Build.props`, so a new warning is a build failure.

## Frontend change (`src/UI/mango-ui`)

1. `pnpm --dir src/UI/mango-ui lint`
2. `pnpm --dir src/UI/mango-ui build`

There is no frontend test runner — do **not** invent test commands.

## After editing `.agent/` harness sources

- `.agent/{skills,agents,workflows}/` changed → `pwsh ./scripts/sync-agent-harness.ps1`
  (verify with `-Check`).
- `.agent/tickets/**/ticket.json` changed → `pwsh ./scripts/update-ticket-board.ps1`
  (verify with `-Check`).

Skills exist for several of these flows: `run-tests`, `fix-warnings`, `review`,
`manage-documentation`.
