---
id: SEC-DEPS-002
kind: security
status: in_progress
priority: high
createdUtc: 2026-11-13T08:36:30Z
updatedUtc: 2026-11-13T08:49:21Z
completedUtc: null
tags: [security, dependencies, aspire, nuget, frontend]
---

<!--
kind: feature | bug | chore | security | refactor | spike
status: not_started | in_progress | awaiting_approval | blocked | completed | abandoned
State lives in this file only. A step is done only when every task under it is checked or
struck through as skipped. Do not maintain progress anywhere else.
-->

# SEC-DEPS-002 — Clear Aspire critical/high NuGet advisories, legacy WebUtilities DoS, and 29 re-accumulated npm advisories

## Plan
*Approved by the user before Step 2 starts.*

### Threat / goal
A fresh scan on master (2026-11-13), three months after SEC-DEPS-001 cleared the npm backlog, finds
**5 vulnerable NuGet packages (4 advisories)** and **29 npm advisories** (16 high, 12 moderate, 1 low).

NuGet (`dotnet list MangoAspire.sln package --vulnerable --include-transitive`):

| Package | Severity | Advisory | Source | Fix |
| --- | --- | --- | --- | --- |
| `Aspire.Dashboard.Sdk.win-x64` 13.4.6 | **Critical** | GHSA-4g86-54fm-h9h8 / CVE-2026-118740 — security feature bypass (published 2026-11-12) | auto-referenced by AppHost | 13.6.5 |
| `Aspire.Hosting.AppHost`, `Aspire.Hosting` 13.4.6 | **High** | GHSA-f7vh-h5g3-7jrh / CVE-2026-120006 — elevation of privilege (published 2026-11-10) | `Directory.Packages.props` | 13.6.5 |
| `MessagePack` 2.5.302 | Moderate | GHSA-qhrr-8q5h-9q3h — nested arrays exceed buffer limits | AppHost → `StreamJsonRpc` 2.25.29 | 2.5.303 (Aspire.Hosting 13.6.5 depends on 2.5.307 directly) |
| `Microsoft.AspNetCore.WebUtilities` 2.3.10 | Moderate | GHSA-6527-9fp8-r4x6 / CVE-2026-118750 — DoS | `Mango.Infrastructure` → legacy `Microsoft.AspNetCore.Http` 2.3.11 | see Scope |

npm (`pnpm --dir src/UI/mango-ui audit`):

| Package | Advisories | Runtime? | In-range fix |
| --- | --- | --- | --- |
| `axios` 1.19.0 | 12 (6 high) | **yes — ships to browser** | 1.21.1 (`^1.19.0`) |
| `follow-redirects` 1.16.0 | 3 high | no — axios Node adapter only | 1.16.3 via axios |
| `vite` 8.2.1 (+ bundled `launch-editor`) | 6 (1 high, 4 moderate, 1 low) | no — dev server | 8.3.8 (`^8.2.1`) |
| `brace-expansion` 1.1.18 / 5.0.9 | 6 (4 high) | no — eslint / minimatch | 1.1.22 / 5.0.13 |
| `zod` 4.4.3 | 1 high | no — `eslint-plugin-react-hooks` 7.0.1 | 4.6.7 (`^3.25.0 \|\| ^4.0.0`) |
| `minimatch` 10.2.6 | 1 moderate | no — typescript-eslint | **none in range** — fix is 11.0.1; latest typescript-eslint 8.72.2 still requires `^10.2.2` |

### Scope
**Backend — dependency/package wiring only.** No feature slice, handler, validator, DTO, route,
`DbContext`, migration or integration event is touched.

1. **Aspire 13.4.6 → 13.6.5** in `Directory.Packages.props` (all nine `Aspire.*` 13.4.6 pins). Align
   the AppHost `Aspire.AppHost.Sdk` declaration (still `9.5.0`) to 13.6.5 if the build accepts it.
2. **Remove `Aspire.Hosting.NodeJs` 9.5.2** — unreferenced in code (the SPA is launched with
   `AddExecutable("mango-ui", "pnpm", …)` at `AppHost.cs:250`), so removing it beats renaming it to
   `Aspire.Hosting.JavaScript`.
3. **Replace legacy ASP.NET Core 2.x packages in `Mango.Infrastructure`.** Bumping
   `Microsoft.AspNetCore.Http` to its newest 2.3.14 does **not** fix it — that build still depends on
   `WebUtilities` 2.3.13. Replace the `Microsoft.AspNetCore.Http` and
   `Microsoft.AspNetCore.Http.Abstractions` package references with
   `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, and drop their two
   `PackageVersion` pins (no other project references them).

**Frontend — `src/UI/mango-ui` manifests only.** `pnpm update` for in-range resolutions; no `.ts` /
`.tsx` change. `eslint-plugin-react-hooks` stays pinned at `~7.0.1` (SEC-DEPS-001 decision).

**Out of scope:** forcing `minimatch` 11 via `pnpm.overrides` (accepted residual, see Risks); the
SEC-DEPS-001 deferred majors; the CI audit gate (separate follow-up).

#### Exposure triage
- **Aspire** — AppHost + dashboard are the local dev orchestrator, so reach is the developer machine
  unless the dashboard is deployed. Critical + one day old + same-major bump → do first.
- **axios** — the only runtime package. `src/hooks/useApiClient.ts` uses a plain `axios.create()`
  (no `adapter`, no `toFormData`, no `maxRedirects`), so the browser uses the XHR adapter. Most of
  the 12 advisories target the Node HTTP/HTTP2/fetch adapters or proxy handling; the
  prototype-pollution gadgets need a separate pollution primitive. Low real exposure, trivial fix.
- **vite / launch-editor** — dev server only; one advisory (GHSA-587h-7wmj-h5pm) is Windows-specific,
  which is the dev platform here.
- **WebUtilities** — a net11.0 web host resolves the shared-framework assembly over the 2.3.10 package
  copy, so runtime use of the vulnerable copy is unlikely; the 2.x packages are still the wrong
  reference for a net11.0 library and keep the scanner red.

### Steps
1. Aspire upgrade + NodeJs removal → `Directory.Packages.props`, `src/Mango.AppHost/Mango.AppHost.csproj`.
2. FrameworkReference swap → `src/Shared/Mango.Infrastructure/Mango.Infrastructure.csproj`,
   `Directory.Packages.props`.
3. Lockfile refresh → `src/UI/mango-ui/package.json`, `src/UI/mango-ui/pnpm-lock.yaml`.

Each lands as its own conventional commit.

### Verification
- `dotnet list MangoAspire.sln package --vulnerable --include-transitive` → no vulnerable packages.
- `dotnet build MangoAspire.sln` green; `dotnet test MangoAspire.sln` green.
- `pnpm --dir src/UI/mango-ui audit` → only the accepted `minimatch` moderate remains.
- `pnpm --dir src/UI/mango-ui lint` + `build` green.
- Manual: AppHost starts and the dashboard loads on 13.6.5 (needs the user's environment).

### Risks and open questions
- **Aspire minor jump 13.4 → 13.6** may change hosting APIs used in `AppHost.cs` — the build is the
  gate; any API break is fixed in AppHost wiring only.
- **SDK alignment** — moving `Aspire.AppHost.Sdk` from 9.5.0 to 13.x can change implicit package
  references; if it conflicts with the explicit `Aspire.Hosting.AppHost` reference under Central
  Package Management, the SDK stays at 9.5.0 and that is recorded as a decision.
- **FrameworkReference** pulls the full ASP.NET Core shared framework into a class library; every
  consumer of `Mango.Infrastructure` is already an ASP.NET Core app, so there is no new runtime cost.
- **minimatch residual** — accepted: lint-time only, exploitation needs attacker-controlled glob
  patterns, and an override would force a major past typescript-eslint's declared range.

## Steps
- [x] **Step 1 — Scope and plan**
  - [x] Define the vulnerability/hardening goal and scope — fresh NuGet + pnpm scan on master
  - [x] Write the plan above
  - [x] Plan approved by the user — 2026-11-13, "open SEC-DEPS-002 and fix it"
- [x] **Step 2 — Execute**
  - [x] Aspire 13.4.6 → 13.6.5, align AppHost SDK, remove unused Aspire.Hosting.NodeJs — SDK
        9.5.0 → 13.6.5 built clean; ASPIRE010 suppressed (see Decisions) — 636e661
  - [x] Replace Microsoft.AspNetCore.Http(.Abstractions) 2.3.11 with FrameworkReference Microsoft.AspNetCore.App — 7260738
  - [x] Refresh mango-ui lockfile in range (pnpm update), keep eslint-plugin-react-hooks ~7.0.1 —
        react/react-dom/@types additionally held on ~19.2 (see Decisions) — d88b652
- [ ] **Step 3 — Verification**
  - [x] Affected build/lint/tests passing — `dotnet build MangoAspire.sln` 0 errors; `dotnet test`
        6 projects, 255 passed / 0 failed; `pnpm lint` exit 0; `pnpm build` green on vite 8.3.8
  - [x] Security check clean (scan/audit shows no new findings) — NuGet: 27/27 projects "no
        vulnerable packages"; pnpm audit 29 → 1 (accepted minimatch moderate)
  - [x] ~~Zero compiler warnings (fix-warnings), if code changed~~ — skipped: no C# source
        changed; warning codes grouped per project show no new compiler warning, only pre-existing
        nullable/analyzer warnings in untouched projects (NU1902 and ASPIRE010 resolved)
  - [ ] Manual: AppHost + dashboard start on Aspire 13.6.5, and SPA route pass (home → product →
        cart → checkout → confirmation → orders) on axios 1.21 / react-router 7.18.5 — needs a
        running AppHost with Docker; awaiting the user
- [ ] **Step 4 — Documentation, memory and completion**
  - [x] Sync `/docs`, if applicable — Aspire section of docs/PACKAGE_MANAGEMENT.md only
  - [x] Conventional commit created — 636e661, 7260738, d88b652, 1a482a9
  - [x] Promote Decisions/Gotchas below into `.agent/memory/domains/`, if non-obvious —
        stack.md, domains/frontend-spa.md, ops/commands.md, index.md
  - [ ] File the CI dependency-audit gate follow-up ticket (see Open Questions)
  - [ ] File the deferred-upgrades follow-up: React 19.3 (+ bundle code-splitting), together with
        the SEC-DEPS-001 majors

## Links
- Branch: fix/sec-deps-002-aspire-npm-advisories
- Commits: 636e661 (Aspire), 7260738 (FrameworkReference), d88b652 (mango-ui lockfile), 1a482a9 (docs + memory)
- PRs:
- Files: Directory.Packages.props, src/Mango.AppHost/Mango.AppHost.csproj,
  src/Shared/Mango.Infrastructure/Mango.Infrastructure.csproj, src/UI/mango-ui/package.json,
  src/UI/mango-ui/pnpm-lock.yaml
- Docs: docs/PACKAGE_MANAGEMENT.md (Aspire inventory + alignment sections)
- Memory: .agent/memory/stack.md, .agent/memory/domains/frontend-spa.md,
  .agent/memory/ops/commands.md, .agent/memory/index.md

## Decisions
**Keep the dashboard on NuGet; suppress ASPIRE010.** Aspire 13.6.5 warns whenever
`AspireUseCliBundle=false` (the default). With it false, the SDK auto-references
`Aspire.Dashboard.Sdk.<rid>` and DCP as NuGet packages at the `Aspire.Hosting.AppHost` version —
the only reason `dotnet list package --vulnerable` saw the critical dashboard advisory. The CLI
bundle would move the dashboard outside NuGet audit, so the default stays and the warning is
suppressed with a comment in the AppHost csproj.

**Remove `Aspire.Hosting.NodeJs` rather than rename it.** Nothing used it; the SPA is an
`AddExecutable` resource.

**FrameworkReference over a WebUtilities pin.** Pinning `Microsoft.AspNetCore.WebUtilities` 2.3.14
explicitly would also have cleared the scanner, but it keeps a net11.0 library on ASP.NET Core 2.x
packages. Every consumer of `Mango.Infrastructure` is an ASP.NET Core app already, so the
framework reference adds no runtime cost.

**Hold React on 19.2 (`~`).** The blanket refresh pulled React 19.3.1, which no advisory needs.
Measured: master 477.71 kB → refresh minus React 494.11 kB → full refresh 523.31 kB. React 19.3 is
~29 kB of the growth and crosses Vite's 500 kB chunk warning. Same rule as SEC-DEPS-001: no runtime
modernization inside a security fix in an app with no frontend tests. `@types/react(-dom)` held too
so types cannot expose 19.3-only APIs.

**Accept the minimatch residual.** Lint-time only; the fix is 11.0.1 and typescript-eslint 8.72.2
(latest) still declares `^10.2.2`, so only a cross-major `pnpm.overrides` entry could force it.

## Gotchas
**`pnpm add pkg@~x.y.z` saves `^x.y.z`.** The default save-prefix rewrites the range; the hold had to
be set by hand followed by `pnpm install`.

**The AppHost SDK pin is separate from Central Package Management.** `Aspire.AppHost.Sdk` sat at
9.5.0 while every package was 13.4.6 — CPM bumps never touch it. The dashboard version follows the
`Aspire.Hosting.AppHost` package, not the SDK.

**`WarningsAsErrors` covers only CS1998/CS4014.** `.agent/memory/stack.md` claimed any warning fails
the build; the solution actually carries ~100 pre-existing nullable warnings. "No new warnings" had to
be judged by grouping warning codes per project, not by a zero count. Memory corrected.

**A worktree with `node_modules` cannot be removed by `git worktree remove` on Windows** (Filename too
long). Delete it with `node -e "fs.rmSync(path,{recursive:true,force:true})"`, then
`git worktree prune`.
## Open Questions
- Should `pnpm audit --audit-level=high` and NuGet audit (`NU1903`/`NU1904` as errors) gate CI? Raised
  by SEC-DEPS-001 and never filed; the 29 npm advisories that re-accumulated in three months are the
  evidence. Recommended as a separate follow-up ticket.

## Blockers
*One `### blk-N — <one-line summary> (severity: low|medium|high, opened: YYYY-MM-DD, resolved: YYYY-MM-DD|open)` heading per blocker.*

## Session Log
*Append-only, newest last. One `### YYYY-MM-DD` heading per working session.*

### 2026-11-13
Scanned master: `dotnet list package --vulnerable` flagged Aspire 13.4.6 (critical + high, published
2026-11-10/12), MessagePack 2.5.302 and WebUtilities 2.3.10; `pnpm audit` found 29 advisories over 6
packages. Traced sources via `project.assets.json`, `pnpm why` and the NuGet/npm registries. User
approved the fix plan; ticket opened on branch `fix/sec-deps-002-aspire-npm-advisories`.

Executed all three fixes as separate commits. Aspire 13.6.5 confirmed safe for the EF Core 10.x
hold first (its net10.0 group targets `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, the existing
pin). AppHost built on SDK 13.6.5; MessagePack resolved to 2.5.307 without a pin. Solution build 0
errors, 255/255 tests passed, NuGet scan clean on all 27 projects. The SPA refresh initially pulled
React 19.3.1 and pushed the bundle past 500 kB; a baseline build of master in a throwaway worktree
plus a React-held rebuild attributed the growth, and React was held on 19.2. Final SPA state: audit
1 moderate (accepted), lint exit 0, build 494.11 kB. Synced the Aspire section of
`docs/PACKAGE_MANAGEMENT.md` and promoted decisions/gotchas into memory. Outstanding: manual AppHost
+ dashboard run and SPA route pass, and the two follow-up tickets.
