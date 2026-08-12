# SEC-CI-001 — Technical Blueprint

*Produced by the `analyze-requirement` workflow, approved by the user before any code is written.
Progress against this plan is tracked in `ticket.json`, not here.*

## Requirement

> "In gitleaks workflow, please add me a trivy step to check package."
> …then: "in .net also use trivy now, please use docker command for trivy."

Add dependency and container vulnerability scanning to CI. This is the gate that
[SEC-DEPS-001](.agent/tickets/SEC-DEPS-001/ticket.json) step-4.3 asked for as a follow-up — that ticket
cleared 38 npm advisories by hand and explicitly left "stop them coming back" to a separate ticket.
Trivy supersedes the `pnpm audit --audit-level=high` idea recorded there.

## Decisions

| # | Decision | Effect |
|---|---|---|
| D1 | .NET covered by **both** `dotnet list package --vulnerable` **and** Trivy | See the revision below — Trivy now scans .NET too |
| D2 | **Yes, scan images** | Container scanning is in scope for this ticket, not a follow-up |
| — | **Separate `trivy.yml`** | [gitleaks.yml](.github/workflows/gitleaks.yml) is **not touched at all** |
| — | **Gate from day one** | HIGH/CRITICAL fails the check; no report-only phase |
| — | **Severity floor** | HIGH,CRITICAL blocks. Everything else still reaches the Security tab |
| — | **Trivy runs from its container image** | `docker run ghcr.io/aquasecurity/trivy:0.73.0`, not `trivy-action` |

The "separate file" decision overrides the literal wording of the original request. It is the better
call: secret scanning needs `fetch-depth: 0` and full history, container scanning needs Docker and image
builds, and the two have wildly different runtimes — coupling them means one re-run always pays for the
other.

### Revision, 2026-08-12: .NET gets Trivy, and Trivy runs via Docker

The first pass concluded Trivy could not see NuGet in this repository, and routed .NET to
`dotnet list package --vulnerable` alone. That conclusion was right about the *working tree* and wrong
as a limit on the ticket. Trivy's `dotnet-core` analyser reads `<Project>.deps.json`, which
`dotnet build` writes into `bin/`. Scanning the build output therefore gives Trivy the fully-resolved
NuGet graph without a single lockfile being committed — and the build already happens in `build.yml`,
so it costs one scan, not one build.

Both .NET signals are kept, and the overlap is deliberate rather than an oversight:

| | Input | Database | When |
|---|---|---|---|
| `dotnet list package --vulnerable` | restore graph | NuGet / GitHub Advisory | before compile |
| `trivy fs src` | `*.deps.json` from `bin/` | Trivy (GHSA + NVD) | after compile |

Different inputs and different databases, so they will disagree at the margins. That is the point;
treating a disagreement as a bug in one of them is the failure mode to avoid.

Running Trivy from its own container removed a class of problem the action introduced: no action to pin,
no annotated-tag SHA trap, no `skip-setup-trivy` version dance. All eight invocations now go through
[.github/scripts/trivy-scan.sh](.github/scripts/trivy-scan.sh), so the severity policy, the cache
location and the DB mirror are defined once.

## Business summary

**What:** a `Dependency & Container Scan` workflow with three jobs (SPA packages, base images,
application images), plus a NuGet audit step and a Trivy scan of the build output in the backend build
job.

**Why:** the repository had no automated dependency gate. CI was `gitleaks` (secrets) and `build.yml`
(compile + test). Advisory drift was invisible until someone audited by hand, which is exactly how
SEC-DEPS-001 accumulated 38 advisories.

## Technical impact map

CI/infrastructure only. **No application code is touched** — no vertical slice, no `DbContext`, no EF
migration, no React component, no `src/UI/mango-ui/src/api/` client. There is consequently no xUnit
coverage to add and no `pnpm lint`/`build` to re-run.

| File | Change |
|---|---|
| [.github/scripts/trivy-scan.sh](.github/scripts/trivy-scan.sh) | **New.** Single definition of how Trivy is invoked |
| [.github/workflows/trivy.yml](.github/workflows/trivy.yml) | **New.** Jobs `npm-dependencies`, `base-images`, `app-images` |
| [.github/workflows/build.yml](.github/workflows/build.yml) | NuGet audit step + Trivy scan of the .NET build output |
| [.trivyignore](.trivyignore) | **New.** Accepted-risk register, applied to gates only |
| [.gitignore](.gitignore) | Ignore `.trivycache/` |
| [.github/workflows/gitleaks.yml](.github/workflows/gitleaks.yml) | **Unchanged.** Do not edit it |

### Coverage map

| Ecosystem | Scanned by | Reads | Runs on |
|---|---|---|---|
| npm (SPA) | `trivy (npm dependencies)` | `pnpm-lock.yaml` | every PR |
| NuGet | `Audit NuGet packages` in `build.yml` | restore graph | every PR |
| NuGet | `Scan .NET build output` in `build.yml` | `bin/**/*.deps.json` | every PR |
| NuGet + OS | `trivy (app image: …)` | `/app/*.deps.json` + image layers | push, weekly, dispatch |
| OS (bases) | `trivy (base image: …)` | 6 pulled images | every PR |

## Implementation shape

### `trivy-scan.sh`

`trivy-scan.sh <sarif|gate> <fs|image|tar> <target> [output.sarif] [extra flags…]`

- **`sarif`** reports every severity and always exits 0. It must not fail: a non-zero exit skips the
  upload step that follows, so the findings that caused the failure never reach the Security tab.
- **`gate`** is HIGH/CRITICAL only, `--exit-code 1`, `--ignore-unfixed`, `--ignorefile .trivyignore`.
  This is what blocks a merge.
- `--user $(id -u):$(id -g)` keeps the SARIF file and the cache owned by the runner. Left as root,
  `actions/cache` cannot read Trivy's `0700` cache directory and the DB is re-downloaded every job.
- `tar` kind scans a `docker save` tarball, so locally-built images are scanned without handing the
  container the Docker socket.

> Unlike the `trivy-action` wrapper, the Trivy **CLI applies `--severity` to SARIF output too**. The
> `sarif` mode therefore passes no `--severity` at all — otherwise MEDIUM and below would silently
> vanish from the Security tab. This is the exact inverse of the action's behaviour and is easy to get
> backwards.

### Job layout

| Job | Targets | Runs on |
|---|---|---|
| `trivy (npm dependencies)` | `src/UI/mango-ui` | every PR |
| `trivy (base image: …)` | 6: aspnet, nginx + 4 compose images | every PR |
| `trivy (app image: …)` | 11 built images | push, weekly cron, dispatch |

`app-images` is excluded from `pull_request`: ten multi-stage .NET builds is minutes of runner time for
a signal that only moves when a dependency or a base image moves. The cheap jobs plus `build.yml` are
the PR gate.

Build stages (`dotnet/sdk`, `node:22-alpine`) are deliberately unscanned — they never ship, so a finding
there cannot reach production and would only teach the team to ignore the job.
`docker/serena/Dockerfile` is local agent tooling, not product.

## Acceptance criteria

1. **Given** a pull request, **when** CI runs, **then** `trivy (npm dependencies)`, the six
   `trivy (base image: …)` jobs and the backend `Scan .NET build output` step all report, **and**
   `app-images` does not run.
2. **Given** a push to `master` or the weekly cron, **when** the workflow runs, **then** `app-images`
   builds and scans all 11 images, `mango-ui` included, with no build-context failures.
3. **Given** the current tree, **when** every gate runs, **then** all are green — the SPA is clean after
   SEC-DEPS-001 and the solution reports zero advisories across 28 projects.
4. **Given** a dependency deliberately downgraded to a known-vulnerable version on a scratch branch,
   **when** CI runs, **then** the relevant gate exits non-zero and the check goes red.
   *This negative test is the only real proof the gate works — a green run proves nothing.*
5. **Given** a NuGet package with a known HIGH advisory, **when** `build.yml` runs, **then** the audit
   step fails before `Build`, or the Trivy scan fails before `Test`.
6. **Given** the `Scan .NET build output` step, **when** it runs, **then** its report lists NuGet
   packages sourced from `*.deps.json` — confirming the .NET graph really is read, not silently skipped.
7. **Given** a finding with no fixed version, **when** a gate runs, **then** it does not block, but the
   finding is still present in the uploaded SARIF.
8. **Given** any scan with findings, **when** the job finishes, **then** SARIF reaches
   Security > Code scanning under a per-job `category`, **and** an upload failure (no GHAS on a private
   repo) does not by itself fail the job.
9. **Given** this change merges, **then** [gitleaks.yml](.github/workflows/gitleaks.yml) is byte-for-byte
   unchanged and the `gitleaks` check keeps its name.

### Testing

- **Backend / frontend:** N/A. No application code changes, so no xUnit and no `pnpm lint`/`build`.
  There is no frontend test runner in this repo and none should be invented.
- **What replaces it:** workflow YAML structure validation, matrix paths checked against disk, the
  audit filter exercised against real `dotnet list package` JSON in both directions, and the script's
  constructed `docker run` command inspected for all six call shapes. Then the CI run itself.

## Risks and open questions

- **Gating from day one means the first red build may be unrelated to the PR that triggers it.** A CVE
  published tonight fails every open PR tomorrow. `ignore-unfixed` and `.trivyignore` are the release
  valves — agree who may add a suppression *before* this lands, or the first inconvenient finding gets
  suppressed by whoever is most blocked.
- **`app-images` runtime.** Ten multi-stage .NET builds. GHA layer caching makes the steady state
  acceptable; the first run is cold and slow. Keeping it off `pull_request` is what makes it affordable.
- **Two floating base tags** (`nginx:alpine`, `rabbitmq:3-management-alpine`) mean scan results are not
  reproducible run to run — the same commit can go green today and red tomorrow.
- **`ignore-unfixed` hides real risk on base images.** Right default for a gate, but the Security tab is
  then the only place unfixed CVEs are visible. Someone has to actually read it.
- **`build.yml` now depends on Docker Hub/ghcr availability** to run its gate. A registry outage turns
  the backend build red for a reason unrelated to the code.

### Open questions (non-blocking — defaults already chosen)

1. Should `app-images` also run on `pull_request` behind a `paths` filter for `**/Dockerfile` and
   `Directory.Packages.props`? Closes the window where a dependency bump merges unscanned.
2. Who owns `.trivyignore` approvals?
3. Pin the two floating base tags by digest here, or separately (tracked at step-4.6)?
4. Keep both .NET signals long-term, or drop `dotnet list package --vulnerable` once Trivy has proven
   itself on the deps.json path?
