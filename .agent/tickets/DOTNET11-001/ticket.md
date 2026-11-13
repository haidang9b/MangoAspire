---
id: DOTNET11-001
kind: chore
status: awaiting_approval
priority: normal
createdUtc: 2026-11-13T00:00:00Z
updatedUtc: 2026-11-13T00:00:00Z
completedUtc: null
tags: [dotnet, dependencies, upgrade]
---

# DOTNET11-001 — Upgrade the solution from .NET 10 to .NET 11

## Plan

### Goal
Move every .NET project from `net10.0` to `net11.0` (GA SDK `11.0.100` is installed locally) and
move the packages that ship on the .NET 11 release train to `11.0.x`, keeping the solution building
and the test suite green.

### Scope
In:
- `<TargetFramework>` `net10.0` → `net11.0` in every `.csproj` under `src/` and `tests/`.
- `Directory.Packages.props`: ASP.NET Core / `Microsoft.Extensions.*` packages → `11.0.0`;
  `Swashbuckle.AspNetCore` → `11.0.1` (forced: `Microsoft.AspNetCore.OpenApi 11` needs
  `Microsoft.OpenApi 3.x`); EF Core stack → latest `10.0.x` patch (see Decisions).
- Dockerfiles (`aspnet`/`sdk` `10.0-alpine` → `11.0-alpine`), CI `setup-dotnet`, devcontainer image,
  Serena MCP image (its C# language server needs the matching SDK).
- Docs and `.agent/` rules/memory/skills that state the target framework; regenerate `.claude/`.

Out:
- EF Core 11 (blocked — see Decisions/Blockers).
- Aspire version bump (13.4.6 → 13.6.5), Semantic Kernel / `Microsoft.Extensions.AI` bump,
  Duende 7 → 8, OpenIddict 7.6 → 7.8 — independent upgrades, not required by .NET 11.
- Existing EF migration `ProductVersion` annotations (historical, never hand-edited).
- Closed ticket artifacts.

### Steps
1. Retarget all `.csproj` files.
2. Update `Directory.Packages.props`.
3. Update Dockerfiles, `.github/workflows/build.yml`, `.devcontainer/devcontainer.json`,
   `docker/serena/Dockerfile`, `docker-compose.mcp.yaml`.
4. `dotnet restore` / `build` / `test`; fix any breaking changes and new warnings.
5. Update docs, `.agent/` sources, `scripts/bootstrap.ps1`; run `scripts/sync-agent-harness.ps1`.

### Verification
- `dotnet build MangoAspire.sln` — 0 errors; no new warnings introduced by the upgrade.
- `dotnet test MangoAspire.sln` — all green.
- `pwsh ./scripts/sync-agent-harness.ps1 -Check` clean.

### Risks and open questions
- C# 15 / ASP.NET Core 11 breaking changes or new obsoletion warnings.
- `Microsoft.OpenApi` 2.x → 3.x API changes surfacing through Swashbuckle 11 in `Mango.Infrastructure`.
- Aspire AppHost SDK (`9.5.0`) / Aspire `13.4.6` hosting a `net11.0` AppHost.

## Steps
- [x] **Step 1 — Scope and plan**
  - [x] Define goal and scope
  - [x] Write the plan above
  - [x] Plan approved by the user — direct request "update .net to latest version (.NET 11)"; the EF Core 10 hold is forced by the ecosystem, not a preference
- [x] **Step 2 — Execute**
  - [x] Retarget `.csproj` files to `net11.0`
  - [x] Update `Directory.Packages.props`
  - [x] Update Dockerfiles, CI, devcontainer, Serena image
  - [x] Fix breaking changes surfaced by the build — none in code; 3 × NU1510 fixed by dropping redundant package refs
- [ ] **Step 3 — Verification**
  - [x] Affected build/lint/tests passing — build 0 errors, 255/255 tests, EF model check clean on all 8 DbContexts
  - [x] ~~Zero compiler warnings (fix-warnings), if code changed~~ — skipped: no `.cs` changed; warning set diffed against a `master` baseline build — zero new warnings
  - [ ] Container images build on `11.0-alpine` and AppHost starts — not run: Docker Desktop not running in the session
- [ ] **Step 4 — Documentation, memory and completion**
  - [x] Sync `/docs`, `.agent/` and regenerate `.claude/`
  - [x] Conventional commit created
  - [x] Promote Decisions/Gotchas below into `.agent/memory/`, if non-obvious — EF Core hold → `.agent/memory/stack.md`

## Links
- Branch: `chore/dotnet-11`
- Commits:
- PRs:
- Files: `Directory.Packages.props`, `src/**/*.csproj`, `tests/**/*.csproj`, `src/**/Dockerfile`
- Docs:
- Memory: `.agent/memory/stack.md`

## Decisions
- **EF Core stays on 10.0.x (latest patch 10.0.15) on top of net11.0.** As of 2026-11-13:
  - `Npgsql.EntityFrameworkCore.PostgreSQL` has no stable 11.x; `11.0.0-rc.3` pins EF Core to
    *exactly* `11.0.0-rc.3.26522.148`, so it cannot be combined with EF Core `11.0.0` GA.
  - `EFCore.NamingConventions 10.0.1` (latest) constrains EF Core to `[10.0.1, 11.0.0)`.
  - `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (13.4.6 and 13.6.5) has no `net11.0` group; on
    `net11.0` it resolves its `net10.0` group → Npgsql EF 10.x.
  - Consequently `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (11.0.0 → EF Relational 11) and
    `MockQueryable.Moq` (11.0.0 → EF 11) also stay on their 10.x lines.
  EF Core 10 targets `net10.0` and runs on the .NET 11 runtime.
- **Swashbuckle.AspNetCore 10.2.3 → 11.0.1** because `Microsoft.AspNetCore.OpenApi 11.0.0` requires
  `Microsoft.OpenApi [3.11.0, 4.0.0)`.
- **Devcontainer image `1-10.0` → `2-11.0`**: no `1-11.0` tag exists on MCR; the image major moved to 2.
- **Dropped explicit refs to `Microsoft.Extensions.Options` (EventBus), `.Logging.Abstractions`
  (Mango.Core), `.DependencyInjection.Abstractions` (Mediator)** and their central versions. On
  `net11.0` the SDK prunes them as platform-provided (NU1510); all three projects compile without
  them. EventBus/Mango.Core did not use those types at all.
- **Serena image installs SDK 11.0 + runtime 10.0**: the projects need the 11 SDK for MSBuild; the
  Roslyn language server's own runtime requirement was 10.0 and it does not roll forward majors.

## Gotchas
- `sed -i` in Git Bash rewrites CRLF files as LF; files without a trailing newline then pick up a
  stray `\r` if you re-add CRs line-by-line, which makes git show whole-file diffs. Use byte-safe
  edits (Python `read_bytes`/`write_bytes`) for bulk changes in this repo.
- `dotnet ef` run from the repo root picks `docker-compose.dcproj` as the startup project; pass
  `--startup-project` explicitly.
- `dotnet format --verify-no-changes` already fails on `master` (CHARSET / FINALNEWLINE in
  `Identity.API`), unrelated to this ticket.
## Open Questions
## Blockers
### blk-1 — EF Core 11 waits on Npgsql.EntityFrameworkCore.PostgreSQL 11 GA and EFCore.NamingConventions 11 (severity: low, opened: 2026-11-13, resolved: open)
Follow-up once both ship stable: bump the EF Core stack, `Identity.EntityFrameworkCore`,
`MockQueryable.Moq` to 11.x, run `dotnet ef migrations has-pending-model-changes` per DbContext.

## Session Log
### 2026-11-13
- Created ticket. Inventory: 27 projects on `net10.0`, 10 Dockerfiles, CI, devcontainer,
  Serena image. Checked NuGet dependency graphs; EF Core 11 blocked (see Decisions).
- Retargeted, bumped packages, updated infra + docs, regenerated `.claude/` (`-Check` clean).
- Verified: `dotnet build` 0 errors; warnings diffed against a `master` (.NET 10) worktree build —
  0 new, 6 × NU1902 (`WebUtilities` in test projects) gone; `dotnet test` 255/255 on `net11.0`;
  `dotnet ef migrations has-pending-model-changes` clean for all 8 DbContexts; `project.assets.json`
  confirms no EF Core 11 resolved anywhere and `Microsoft.OpenApi` unified on 3.11.0.
- Not verified: Docker image builds / AppHost run (Docker Desktop not running).
- Pre-existing, out of scope: NU1902–NU1904 advisories on `Aspire.Hosting*` 13.4.6 (incl. a
  critical on `Aspire.Dashboard.Sdk`), `MessagePack` 2.5.302, `Microsoft.AspNetCore.WebUtilities`
  2.3.10 in `Mango.Infrastructure` — candidate follow-up ticket.
