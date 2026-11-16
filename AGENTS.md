# AGENTS.md

MangoAspire: .NET 11 + Aspire microservices (Vertical Slice), a React/Vite SPA in `src/UI/mango-ui`,
PostgreSQL, RabbitMQ, Debezium. The dev machine runs Windows + PowerShell; scripts are in `scripts/`.

## Commands

- Restore / build: `dotnet restore MangoAspire.sln` · `dotnet build MangoAspire.sln`
- Run: `dotnet run --project src/Mango.AppHost/Mango.AppHost.csproj`
- Test: `dotnet test MangoAspire.sln`. For one project, pass its `.csproj`; for one test, add
  `--filter "FullyQualifiedName~Class.Method"`.
- Format: `dotnet format --verify-no-changes MangoAspire.sln` (`WarningsAsErrors` is on)
- SPA, from the repo root: `pnpm install --dir src/UI/mango-ui` · `pnpm --dir src/UI/mango-ui dev|build|lint`.
  There is no frontend test runner, so verify with lint + build and never invent test commands.

## Guardrails

- Vertical Slice Architecture by feature (`Features/` + `Routes/`). No Clean/Onion/N-tier layers.
- No repository layer: `DbContext` directly in handlers. Only `Mango.Orchestrators` keeps one, for its saga.
- No cross-slice calls; share infrastructure only.
- Thin `Program.cs`: `AddApiDefaults()` / `UseApiPipeline()` / `MigrateDatabaseAsync()`.
- Handlers return `ResultModel<T>`. Business errors throw `DataVerificationException`, which
  `GlobalExceptionHandler` turns into ProblemDetails.
- Every command has a FluentValidation validator. The in-house mediator (`src/Shared/Mediator`)
  runs behaviours in this order: Logging → Validation → Tx.
- Package versions only in `Directory.Packages.props`. pnpm, never npm, for the SPA.
- TypeScript is strict; no `any`.
- Tests use xUnit + Moq + Shouldly, named `Method_When_Condition_Then_Result`.
- Keep changes minimal: don't touch unrelated files or introduce new patterns unasked.

## Where guidance lives

- **Rules** load automatically by file path from `.claude/rules/`; Copilot reads the generated
  mirror in `.github/instructions/`. Never hand-edit `.github/instructions/` or `.github/agents/`.
  Regenerate them with `scripts/sync-agent-harness.ps1`, and lint with its `-Check` switch.
- **Skills** live in `.claude/skills/`. The human-run workflows are `/implement-ticket`,
  `/analyze-requirement`, `/onboarding`, `/update-docs` and `/fix-warnings`.
- **Memory** (what is true, and lessons learned): start at `.agent/memory/index.md`, then read only the
  domain files you need.
- **Done checklist:** `.agent/memory/ops/task-completion.md`.

## Specs and tickets

- Feature work gets a spec in `specs/<slug>/` (copy `specs/_template/`): `spec.md`,
  `acceptance-criteria.md` and `test-plan.md`. Tick an AC only with cited evidence.
- Track any multi-step task in `.agent/tickets/<TICKET-ID>/ticket.md`, a single file:
  - Front-matter holds `kind` / `status`; `## Steps` checkboxes are progress; the rest is prose.
  - `kind` (`feature | bug | chore | security | refactor | spike`) picks the template in
    `.agent/tickets/_templates/`.
  - Resume at the first unchecked step, then its first unchecked task.
  - A skipped task is written `- [x] ~~title~~ — skipped: <reason>`.
