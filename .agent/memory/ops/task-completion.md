# Task Completion Checklist

Run the smallest valid command set for the area you changed. Do not modify unrelated files and do
not introduce new architecture patterns without an explicit request.

## Backend change

1. `dotnet build MangoAspire.sln` (or just the affected `.csproj`).
2. Affected tests: `dotnet test tests/Services/<Area>.API.Tests/<Area>.API.Tests.csproj`
   (optionally `--filter "FullyQualifiedName~..."`).
3. Formatting: `dotnet format --verify-no-changes MangoAspire.sln` — run `dotnet format` to fix.

`Directory.Build.props` makes only CS1998 and CS4014 build errors. About 100 nullable warnings
already exist, so check that you added none (compare against a baseline build) rather than
expecting zero.

## Frontend change (`src/UI/mango-ui`)

1. `pnpm --dir src/UI/mango-ui lint`
2. `pnpm --dir src/UI/mango-ui build`

There is no frontend test runner — do **not** invent test commands.

## After editing the harness

- `.claude/rules/` or `.claude/agents/` changed: run `pwsh ./scripts/sync-agent-harness.ps1`, then
  `-Check`.
- `.claude/skills/`, `AGENTS.md`, `.agent/memory/` or `specs/` changed: run
  `pwsh ./scripts/sync-agent-harness.ps1 -Check` (Facts, Links, Specs, Budget).
- A skill changed substantively: re-run its evals with `scripts/eval-skills.ps1 -Skill <name>` and
  compare the verdict.
- `.agent/tickets/**/ticket.md` changed: nothing to regenerate; the file is the state.

Windows PowerShell 5.1 has no `pwsh`: run
`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1` (add `-Check`).

Skills cover several of these flows: `run-tests` and `generate-commit`, plus `/fix-warnings` and
`/update-docs`.
