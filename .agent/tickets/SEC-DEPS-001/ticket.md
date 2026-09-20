---
id: SEC-DEPS-001
kind: security
status: in_progress
priority: high
createdUtc: 2026-08-12T00:00:00Z
updatedUtc: 2026-08-12T00:00:00Z
completedUtc: null
tags: [security, dependencies, frontend]
---

# SEC-DEPS-001 — Clear 38 npm advisories in mango-ui via lockfile refresh and react-router-dom bump

## Plan

### Threat / goal
Clear the 25 open GitHub Dependabot alerts raised against `src/UI/mango-ui/pnpm-lock.yaml`
(24 High/Moderate + 2 Low). A local `pnpm audit` reports **38** advisories across 9 packages —
Dependabot deduplicates some resolution paths and does not surface `nanoid` at all, so the audit
count is the authoritative target.

The root cause is a **stale lockfile, not a version-constraint problem**. Every flagged package's
existing semver range in `package.json` already permits a patched version; the lockfile was simply
never refreshed. Only `react-router-dom` needs an actual manifest bump.

Acceptance criteria:
- `pnpm --dir src/UI/mango-ui audit` reports 0 high and 0 moderate advisories.
- `pnpm --dir src/UI/mango-ui build` succeeds (`tsc -b` + `vite build`), no new type errors.
- `pnpm --dir src/UI/mango-ui lint` exits clean, no new rule violations.
- `git diff --stat` shows only `package.json` and `pnpm-lock.yaml`.
- Manual route pass: home → product details → cart → checkout → confirmation → orders all render,
  with `useParams` / `useSearchParams` resolving correctly.
- No .NET change, therefore no new xUnit tests and no `dotnet build` required.

### Scope
**Not applicable (backend).** No service, feature slice, command/query, handler, validator, DTO,
route, `DbContext` configuration, EF Core migration, or integration event is touched by this ticket.

**Frontend:**
- **Components:** none. No `.ts` / `.tsx` source change is anticipated.
- **Manifest:** `react-router-dom` `^7.13.0` → `^7.18.2`; `vite` moved off the `^8.0.0-beta.13`
  prerelease pin onto stable `^8.2.1`, and the now-redundant `pnpm.overrides` block removed.
- **Lockfile:** refreshed so in-range transitive resolutions pick up their published patches.

#### Exposure triage
| Tier | Packages | Alerts | Real exposure |
| --- | --- | --- | --- |
| Runtime (ships to browsers) | `react-router` / `react-router-dom` 7.13.0 | 11 | 1 genuinely applicable |
| Build-time only (`Development`) | `postcss`, `nanoid`, `flatted`, `minimatch`, `brace-expansion`, `js-yaml`, `picomatch`, `@babel/core` | 27 | Low — needs attacker-controlled globs/YAML/CSS in a trusted repo |

`src/App.tsx` uses plain `BrowserRouter` + `Routes` / `Route`. There is no `createBrowserRouter`,
no data router, no loaders or actions, no SSR, no RSC, no framework mode, and no
`__manifest` / single-fetch endpoint. The turbo-stream RCE (#34), `__manifest` DoS (#35),
single-fetch DoS (#40), RSC CSRF (#68), RSC XSS (#32, #56), SSR hydration injection (#55),
prerendered `Location` XSS (#31) and document-request CSRF (#43) all require a React Router
*server*, which this SPA does not have — they are unreachable here.

The one advisory that touches live code is **#57, open redirect via backslash in `<Link>` /
`useNavigate`** (fixed in >= 7.18.0). Even that is minimal today: every navigation target is a
hardcoded constant from `src/constants/routes.ts` or an interpolated id
(`` `${ROUTES.ORDERS}/${order.id}` ``), so no user-controlled target exists.

#### Resolution evidence
| Package | Locked | Range in manifest | Min fix | In-range resolve | Advisories cleared |
| --- | --- | --- | --- | --- | --- |
| `react-router(-dom)` | 7.13.0 | `^7.13.0` | 7.18.2 | needs manifest bump to `^7.18.2` | 11 |
| `postcss` | 8.5.6 | `^8.x` via vite | 8.5.23 | 8.5.26 | 4 |
| `nanoid` | 3.3.11 | `^3.3.11` via postcss | 3.3.17 | 3.3.18 | 2 |
| `flatted` | 3.3.3 | `^3.2.9` via flat-cache | 3.4.2 | 3.4.4 | 2 |
| `minimatch` (3.x) | 3.1.3 | `^3.1.2`; eslint 9.39.5 pins `^3.1.5` | 3.1.4 | 3.1.5 | 2 |
| `minimatch` (9.x) | 9.0.6 | `^9.0.4` via ts-estree | 9.0.7 | 9.0.9 | 2 |
| `brace-expansion` (1.x) | 1.1.12 | `^1.1.7` | 1.1.18 | 1.1.18 | 4 |
| `brace-expansion` (5.x) | 5.0.2 | `^5.x` | 5.0.9 | 5.0.9 | 4 |
| `js-yaml` | 4.1.1 | `^4.1.0` | 4.3.1 | 4.3.1 | 3 |
| `picomatch` | 4.0.3 | `^4.0.x` | 4.0.4 | 4.0.5 | 2 |
| `@babel/core` | <= 7.29.0 | `^7.x` | 7.29.6 | 7.29.7 | 1 |

No `pnpm.overrides` entries are needed to force patched transitives, and no major-version upgrade
is required.

### Verification
`pnpm --dir src/UI/mango-ui audit` clean, `pnpm lint` + `pnpm build` green, `git diff --stat`
limited to the two manifest files, plus a manual route pass.

### Risks and open questions
**Decided (user approved the recommended scope):**
1. **Minimal fix, majors deferred.** `pnpm outdated` also shows waiting majors — eslint 10,
   `@vitejs/plugin-react` 6, TypeScript 7, i18next 26, react-i18next 17, `@types/node` 26. These
   are excluded; the security fix lands as its own commit and the majors get a separate ticket.
2. **`@vitejs/plugin-react` v6 excluded.** v6 adds two *required* new peers
   (`@rolldown/plugin-babel`, `babel-plugin-react-compiler`), which is a config change rather than
   a version bump, and it is not needed for any advisory.
3. **Vite prerelease pin dropped.** Vite 8.2.1 is stable and satisfies `^8.0.0-beta.13`, so the
   refresh lands on stable regardless; the `pnpm.overrides` block becomes dead weight and is
   removed.

**Risks:**
- `react-router-dom` 7.13.0 → 7.18.2 is a five-minor jump. Same major and the consumed surface
  (`BrowserRouter`, `Routes`, `Route`, `Link`, `Navigate`, `useNavigate`, `useParams`,
  `useSearchParams`) is stable across the v7 line, but `build` + a manual route pass is required
  because there is **no frontend test runner** in this repo.
- Moving vite off the prerelease pin changes the build toolchain version. Mitigated by the fact
  that `^8.0.0-beta.13` already resolved forward to 8.x, and `build` verifies it.
- A blanket lockfile refresh also advances unrelated in-range packages (`react`, `axios`,
  `oidc-client-ts`, `react-hook-form`, `@types/*`). Acceptable — all are patch/minor within their
  declared ranges — but it widens the lockfile diff beyond the strictly security-relevant lines.

**Still open:**
- Should `pnpm audit --audit-level=high` gate CI? `.github/workflows/build.yml` has no audit step,
  so this backlog can silently re-accumulate. Recommended as a **separate** follow-up ticket.

## Steps
- [x] **Step 1 — Business analysis and technical planning**
  - [x] Run the analyze-requirement sub-workflow
  - [x] Triage all 38 pnpm audit advisories: runtime vs build-only exposure — 11 react-router
        advisories; only #57 open redirect is reachable in a BrowserRouter-only SPA
  - [x] Prove every fix is reachable in-range (resolution evidence table) — only react-router-dom
        needs a manifest bump; the rest are stale-lockfile
  - [x] Write the technical blueprint above
  - [x] Present clarifying questions to the user
  - [x] Blueprint approved by the user — recommended minimal scope; majors deferred
- [x] **Step 2 — Implementation (frontend dependencies only)**
  - [x] Bump react-router-dom to ^7.18.2 in package.json
  - [x] Move vite off the ^8.0.0-beta.13 prerelease pin to stable ^8.2.1 — resolved to vite 8.2.1
        stable; build verified on it
  - [x] Remove the now-redundant pnpm.overrides block
  - [x] Refresh pnpm-lock.yaml so in-range transitives pick up their patches — `pnpm update` also
        rewrote manifest range floors to the resolved minors; no majors crossed
  - [x] Pin eslint-plugin-react-hooks to ~7.0.1 to hold its new rule set out of scope — 7.1.1 adds
        purity/refs/set-state-in-effect, surfacing 6 pre-existing errors; plugin has zero
        advisories so pinning costs nothing security-wise; cleanup tracked in step 4
  - [x] ~~Backend slice work~~ — skipped: no .NET change, this ticket touches only
        src/UI/mango-ui dependency manifests
- [ ] **Step 3 — Verification and quality**
  - [x] pnpm audit reports 0 high and 0 moderate advisories — 38 advisories to "No known
        vulnerabilities found"
  - [x] pnpm lint and pnpm build passing for the SPA — lint exit 0; build green on vite 8.2.1,
        200 modules, identical bundle hash
  - [x] git diff confirms only package.json and pnpm-lock.yaml changed — 2 files changed, zero
        source files
  - [ ] Manual route pass: home, product details, cart, checkout, confirmation, orders — requires
        a running AppHost; awaiting the user
  - [x] ~~Backend tests~~ — skipped: no .NET change, so no xUnit coverage is applicable
- [ ] **Step 4 — Documentation, memory and completion**
  - [x] Conventional commit created — 42f5e55 on branch fix/sec-deps-001-npm-advisories
  - [ ] File the deferred-majors follow-up ticket (eslint 10, plugin-react 6, TS 7, i18next 26)
  - [ ] File the CI audit-gate follow-up ticket (pnpm audit --audit-level=high)
  - [ ] Follow-up: adopt eslint-plugin-react-hooks 7.1.1 and fix the 6 purity/refs/effect errors —
        CartContext.tsx:24; useFetch.ts:46,62,77; CheckoutPage.tsx:42,150
  - [ ] Promote Decisions/Gotchas below into .agent/memory/domains/frontend-spa.md
  - [x] ~~Sync /docs with the implementation~~ — skipped: dependency-only change, no documented
        behaviour or API surface moved

## Links
- Branch: fix/sec-deps-001-npm-advisories
- Commits: 42f5e55
- PRs:
- Files: src/UI/mango-ui/package.json, src/UI/mango-ui/pnpm-lock.yaml
- Docs:
- Memory: .agent/memory/domains/frontend-spa.md

## Decisions
**Refresh the lockfile rather than pin `pnpm.overrides`.** Every flagged transitive already had a
patched version inside its existing semver range, so forcing versions through `overrides` would
have added permanent maintenance debt to solve a problem a plain reinstall solves. Overrides are
reserved for the case where an upstream range genuinely excludes the fix — which did not occur here
for any of the 9 packages.

**Minimal security scope; majors deferred.** `pnpm outdated` showed eslint 10, `@vitejs/plugin-react`
6, TypeScript 7, i18next 26, react-i18next 17 and `@types/node` 26 waiting. None of them is required
by any advisory, so folding them in would have mixed a low-risk security fix with a high-risk
modernization in one commit — and with no frontend test runner in this repo, that is a bad trade.

**`@vitejs/plugin-react` held at v5.** v6 adds two *required* new peers (`@rolldown/plugin-babel`,
`babel-plugin-react-compiler`). That is a build-config migration, not a version bump.

**Dropped the vite prerelease pin.** Vite 8.2.1 is stable and already satisfies the old
`^8.0.0-beta.13` range, so the refresh landed on stable regardless of intent. Leaving a prerelease
specifier in the manifest afterwards would have been misleading, and the paired `pnpm.overrides`
entry became dead weight.

**Held `eslint-plugin-react-hooks` at `~7.0.1`.** The in-range refresh pulled 7.1.1, which ships
three new rules (`react-hooks/purity`, `react-hooks/refs`, `react-hooks/set-state-in-effect`) and
immediately failed `pnpm lint` with 6 errors — all pre-existing code, none introduced by this
ticket. Fixing them means refactoring the cache/state flow in `useFetch` and the effect in
`CartContext`, with no frontend test runner to catch a regression. That is a quality ticket, not a
security one, so the plugin is pinned to the exact rule set that was in force when this scope was
approved. The plugin carries zero advisories, so pinning costs nothing security-wise, and the
cleanup is tracked as a follow-up rather than suppressed.

## Gotchas
**Dependabot's alert count understates the real one.** GitHub reported 25 alerts; local
`pnpm audit` reported **38** across the same lockfile. Dependabot deduplicates resolution paths and
did not surface `nanoid` at all (2 advisories, reached via `postcss` → `nanoid`). Treat `pnpm audit`
as the authoritative target when closing out a Dependabot backlog — clearing the GitHub list alone
would have left `nanoid` vulnerable.

**Most react-router advisories are unreachable in this SPA.** 11 of the alerts were against
`react-router`, but `src/App.tsx` uses plain `BrowserRouter` + `Routes` / `Route`: no
`createBrowserRouter`, no data router, no loaders/actions, no SSR, no RSC, no framework mode, no
`__manifest` or single-fetch endpoint. Only the backslash open-redirect in `<Link>` /
`useNavigate` touches live code — and even that is inert today because every navigation target is a
hardcoded constant. Severity labels on a Dependabot alert describe the *package*, not this
application's exposure.

**Two independent major lines needed patching for the same CVE class.** `minimatch` and
`brace-expansion` each appear twice in the tree at different majors (3.x via eslint, 9.x via
`@typescript-eslint/typescript-estree`; 1.x and 5.x respectively), with *separate* advisories and
*separate* fix floors. Checking only the newest copy would have left the other vulnerable.

## Open Questions
- Should `pnpm audit --audit-level=high` gate CI in `.github/workflows/build.yml`? Recommended as a
  separate follow-up ticket so this backlog cannot silently re-accumulate.

## Blockers
None.

## Session Log

### 2026-08-12
Ran the `analyze-requirement` workflow against a paste of 25 Dependabot alerts. Established via
`pnpm audit --json`, `pnpm why` and `npm view` that the real count is 38 advisories over 9 packages,
and that every fix except `react-router-dom` is reachable without touching `package.json`. Triaged
react-router exposure against actual SPA usage. Wrote the blueprint above, presented five clarifying
questions, and the user approved the recommended minimal scope.

Executed: bumped `react-router-dom` to `^7.18.2`, moved `vite` to stable `^8.2.1`, deleted the
`pnpm.overrides` block, and ran `pnpm update`. `pnpm audit` went from 38 advisories to **"No known
vulnerabilities found"**. `pnpm update` also rewrote the manifest range floors to the resolved
minors (axios 1.19.0, react 19.2.8, oidc-client-ts 3.5.0, react-hook-form 7.85.0, typescript-eslint
8.67.0 and others) — wider than the blueprint anticipated, but no major boundary was crossed.

First lint run failed on 6 errors from the new `eslint-plugin-react-hooks` 7.1.1 rules; pinned the
plugin to `~7.0.1` per the scope decision above. Final state: `audit` clean, `lint` exit 0, `build`
green on vite 8.2.1 producing a byte-identical bundle hash (`index-kgYFktrP.js`), and
`git diff --stat` limited to `package.json` + `pnpm-lock.yaml` with zero source files touched.
Manual route pass and the follow-up tickets are still outstanding.

### 2026-09-20 (harness migration)
Ticket state moved from the JSON `ticket.json` + `notes.md` + `plan.md` trio into this single
`ticket.md`. No progress changed in the move.
