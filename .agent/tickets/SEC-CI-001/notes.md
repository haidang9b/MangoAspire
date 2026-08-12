# SEC-CI-001 — Notes

> State lives in `ticket.json`. Do not restate status here and do not use `- [ ]` checkboxes —
> this file has no authority over progress, and the board script warns if it tries to claim any.

## Decisions

- **Separate `trivy.yml`, gitleaks.yml untouched** (user, 2026-08-12). This overrides the literal
  wording of the request. Secret scanning needs `fetch-depth: 0` and full history; container scanning
  needs Docker and image builds; their runtimes differ by an order of magnitude. Coupling them means
  one re-run always pays for the other.
- **.NET coverage is D1 B + C, not lockfiles.** `dotnet list package --vulnerable` in `build.yml` for
  PR-time signal, and `trivy image` for the shipped graph. Trivy's filesystem scan of the *working tree*
  is scoped to `src/UI/mango-ui` and its job is named `trivy (npm dependencies)` so a green check cannot
  be read as covering .NET.
- **Revised 2026-08-12 — Trivy also scans .NET, from the build output.** The original conclusion
  ("Trivy cannot see NuGet here") was true of the working tree and wrong as a limit on the ticket:
  `dotnet build` writes `<Project>.deps.json` into `bin/`, which Trivy's `dotnet-core` analyser reads.
  Scanning `src` after the Build step in `build.yml` gives the fully-resolved NuGet graph with no
  lockfile committed and no second compile — the build is already there. Both .NET signals are kept:
  they read different inputs (restore graph vs resolved output) against different databases (NuGet
  advisory vs GHSA+NVD), so disagreement between them is expected, not a defect.
- **Trivy runs from its own container image, not `trivy-action`** (user request). This removed a whole
  class of problem — nothing to pin as an action, no annotated-tag SHA trap, no `skip-setup-trivy`
  version dance — at the cost of `build.yml` now depending on registry availability to run its gate.
- **One script, eight call sites.** `.github/scripts/trivy-scan.sh` defines the severity policy, cache
  location and DB mirror once. Copy-pasting a `docker run` line into eight steps is how the SARIF and
  gate flags drift apart, and that drift is invisible until something is not being scanned.
- **Two Trivy invocations per job, not one.** The SARIF step runs with `exit-code: "0"` and the gate is
  a separate step. Collapsing them into one step with `exit-code: "1"` is the obvious-looking
  simplification and it breaks the feature: a non-zero exit skips the upload step, so the findings that
  caused the failure never reach the Security tab. The second invocation costs a process, not a DB
  download.
- **`scanners: vuln` only.** Trivy's `secret` scanner would duplicate gitleaks findings from the sibling
  workflow; `misconfig` and `license` are separate concerns with separate triage owners.
- **`app-images` is excluded from `pull_request`.** Ten multi-stage .NET builds per push is not worth
  paying on every PR for a signal that only moves when a dependency or base image moves. The two cheap
  jobs are the PR gate; `app-images` runs on push to master, the weekly cron, and manual dispatch.
- **Build-stage bases are not scanned.** `mcr.microsoft.com/dotnet/sdk:10.0-alpine` and `node:22-alpine`
  never ship, so a CVE in them cannot reach production and would only add noise to a gating job.
  `docker/serena/Dockerfile` is local agent tooling, not product.
- **Reuse the gitleaks workflow's non-fatal SARIF pattern** (`continue-on-error` + `hashFiles` guard).
  That file already documents why: SARIF upload needs GitHub Advanced Security on private repos, and
  the job should not go red just for that.

## Gotchas

- **A filesystem Trivy scan sees zero NuGet packages in this repository.** Its NuGet analyzers need
  `packages.lock.json`, `packages.config`, a post-build `*.deps.json`, or a `Version=` attribute on a
  `PackageReference`. There is no lockfile anywhere, `Directory.Build.props` does not set
  `RestorePackagesWithLockFile`, and Central Package Management moves all 67 versions into
  `Directory.Packages.props`, which Trivy does not read. A `trivy fs .` at repo root would report the
  .NET side clean because it found nothing to scan — a false green, not a pass. This is why the scan is
  scoped rather than repo-wide.
- **Scanning images is what recovers the NuGet graph.** `dotnet publish` writes `<Project>.deps.json`
  into `/app`, and `trivy image` reads it — fully resolved, transitives included, exactly as shipped.
  AC 6 exists to prove this actually happens rather than being assumed.
- **`dotnet list package --vulnerable` exits 0 even when it finds vulnerabilities.** The exit code
  cannot be the gate; the output has to be inspected. Use `--format json` (needs the .NET 9+ SDK; this
  repo is on 10.0.x) and `jq` rather than grepping the table, which will false-positive on any package
  whose name contains "High".
- **Build contexts are not uniform.** From `docker-compose.yml`: the ten .NET services build with
  `context: src` and a dockerfile path relative to it; `mango-ui` alone uses
  `context: src/UI/mango-ui`. A matrix that assumes one pattern fails on the odd one out.
- **All ten .NET services share the same final base**, `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`.
  Scanning built images on every PR would report the identical OS CVE list ten times.
- **`ignore-unfixed: true` is load-bearing on the image jobs.** Distro bases routinely carry HIGH CVEs
  with no fixed package published. Without it a gating job goes red on day one for something nobody can
  act on, and the team learns to ignore the check within a week.
- **`aquasecurity/trivy-action@master` is what the upstream README shows.** Unpinned; do not copy it in.
- **`skip-setup-trivy` was added mid-0.29.x.** Confirm it exists on whichever release gets pinned; drop
  the input if not.
- **ghcr.io rate-limits the vulnerability DB pull** for anonymous CI. This is the usual cause of Trivy
  jobs failing with no code change. `TRIVY_DB_REPOSITORY` points at the unthrottled ECR mirror.
- **`fetch-depth: 0` is a gitleaks requirement, not a Trivy one.** Trivy scans the working tree.
  Copying the checkout block from the gitleaks workflow just makes the clone slower.
- **A dependency gate goes red without anyone touching the code.** A CVE published tonight fails every
  open PR tomorrow. `ignore-unfixed` and `.trivyignore` are the release valves.
- **`nginx:alpine` and `rabbitmq:3-management-alpine` are floating tags**, so image scan results are not
  reproducible run to run — the same commit can go green today and red tomorrow.

### Found during implementation

- **`dotnet list package --vulnerable` exits 1 on this solution every single time.** Not because of
  vulnerabilities — `docker-compose.dcproj` is a member of `MangoAspire.sln` and uses `packages.config`,
  which the command cannot analyse, so it emits a `problems[]` entry and returns 1. The first draft of
  the audit step used `set -euo pipefail`, which would have made the backend build permanently red from
  the moment it merged. The step now ignores the exit code and decides from the JSON, surfacing
  `problems[]` as `::warning::` lines so a genuinely unanalysable service still leaves a trace.
- **Confirmed empirically, in the same run:** the command exits **0** when it *does* find a High
  advisory. So the exit code is unusable in both directions — it is 1 when nothing is wrong and 0 when
  something is. The JSON is the only trustworthy signal.
- **Real JSON shape** (probed with a scratch project pinned to `Newtonsoft.Json 12.0.3`):
  `severity` is title-case `"High"`, and the URL key is all-lowercase `advisoryurl`. Projects with no
  findings carry only `path` — no `frameworks` key at all, which is why the filter needs
  `.frameworks[]?` rather than `.frameworks[]`.
- **Baseline is clean:** 28 projects, zero advisories at any severity. The gate should be green on the
  first run, matching the SPA's post-SEC-DEPS-001 state.
- **Unquoted matrix job names broke the YAML.** `name: trivy (base image: ${{ matrix.id }})` contains
  `": "`, which the parser reads as a nested mapping. Both matrix job names are quoted now.
- **The Trivy CLI and `trivy-action` treat SARIF severity filtering in opposite ways**, and getting it
  backwards silently loses findings either way:
  - The **action** ignores `severity` for SARIF unless `limit-severities-for-sarif: true` — so a
    `severity:` line on a SARIF step is inert.
  - The **CLI** applies `--severity` to SARIF like any other format — so the same line there *does*
    filter, and everything below HIGH disappears from the Security tab.

  This repo uses the CLI, so `sarif` mode in `trivy-scan.sh` passes no `--severity` at all. Do not
  "tidy" it in to match the gate.
- **Trivy's cache directory is `0700`.** Run the container as root and `actions/cache` (running as the
  runner account) cannot read it, so the DB silently re-downloads on every job. `--user $(id -u):$(id -g)`
  fixes it and also keeps the SARIF output readable by the upload step.
- **`docker save` tarballs beat socket mounting.** Scanning a locally-built image with
  `trivy image --input image.tar` avoids giving the Trivy container the Docker socket, which is a
  privileged mount for something whose whole job is parsing untrusted package metadata.
- **Extra flags were silently dropped by the first draft of the script.** `--skip-dirs` was passed as
  argument 4, which the script consumed as the SARIF output path. Fixed by shifting the known
  positionals and forwarding `"$@"`; the difference is invisible in the workflow log, because Trivy
  simply scans more than intended and still passes.
- **The script is invoked as `bash .github/scripts/trivy-scan.sh`, not directly.** This repo is
  developed on Windows, where git's executable bit does not survive reliably; relying on it produces a
  "permission denied" that only appears in CI.

### Superseded

- The first implementation used `aquasecurity/trivy-action` and was replaced with `docker run` on user
  request. One trap is worth keeping in case the action ever returns: **`v0.36.0` is an annotated tag**,
  so `git/ref/tags/v0.36.0` returns the *tag object* SHA (`a9c7b0f…`), which Actions will not accept in
  a `uses:` clause. It must be dereferenced to the commit (`ed142fd…`).

## Open Questions

- Should `app-images` also run on `pull_request` behind a `paths` filter for `**/Dockerfile` and
  `Directory.Packages.props`? Closes the window where a dependency bump merges unscanned.
- Who approves a `.trivyignore` suppression?
- Pin the two floating base tags by digest in this ticket, or separately (tracked at step-4.6)?

## Blockers

*None.*

## Session Log

### 2026-08-12

Ran `analyze-requirement` against "in gitleaks workflow, please add me a trivy step to check package".
Read both workflow files, all 12 Dockerfiles, `docker-compose.yml` and the dependency-manifest layout.
First draft assumed a job inside `gitleaks.yml`; the load-bearing finding was the NuGet blind spot,
which turned "add a Trivy step" into a decision about whether .NET gets scanned at all.

User answered: D1 = B + C, D2 = yes scan images, separate `trivy.yml`, gate on HIGH/CRITICAL from day
one. Blueprint rewritten against those choices — it is now three Trivy jobs plus one `build.yml` step,
and `gitleaks.yml` is explicitly out of scope. Choosing D2 turned out to close the NuGet gap that D1
option C leaves open, via `deps.json` inside the built images, so the two decisions combine better than
either does alone. Supersedes SEC-DEPS-001 step-4.3.

Implemented step 2 in full: new `trivy.yml` (3 jobs, 17 scan targets), the NuGet audit step in
`build.yml`, and `.trivyignore`. `gitleaks.yml` untouched, confirmed by an empty diff.

Verification is partial and the gap is deliberate, not forgotten. What ran locally: YAML structure for
all three workflows, every app-image context and dockerfile path against disk, and the audit filter
against real `dotnet list package` JSON in both directions — clean solution passes, a real High
advisory fails. What could not run: anything needing Trivy or Docker, neither of which is available on
this machine, so steps 3.2–3.8 are still open and this has not yet been proven end to end in CI.

Two implementation findings were load-bearing enough to have shipped a broken gate: the `dotnet list
package` exit-code inversion (see Found during implementation) and the annotated-tag SHA. Both are
fixed in the committed YAML.

Not committed — the working tree is still on `fix/sec-deps-001-npm-advisories`, which belongs to
SEC-DEPS-001. This ticket needs its own branch before step-4.1.

Second pass, same day, on user instruction: ".net also use trivy now, please use docker command for
trivy." Replaced every `trivy-action` usage with `docker run ghcr.io/aquasecurity/trivy:0.73.0` behind
a new `.github/scripts/trivy-scan.sh`, and added a Trivy scan of the .NET build output to `build.yml`.
The `deps.json` route means the .NET blind spot that shaped the whole first analysis is now closed on
pull requests too, not only on the weekly image scan.

Verified by dry-running the script with the `docker run` replaced by an echo and inspecting the
constructed argv for all six call shapes. That caught the dropped-`--skip-dirs` bug, which would have
scanned the SPA twice under the .NET category and still reported green. Trivy itself still has not
executed anywhere — no Docker daemon on this machine — so steps 3.2 to 3.8 remain open.
