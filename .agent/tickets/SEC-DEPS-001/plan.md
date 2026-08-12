# SEC-DEPS-001 — Technical Blueprint

*Produced by the `analyze-requirement` workflow, approved by the user before any code is written.
Progress against this plan is tracked in `ticket.json`, not here.*

## Requirement

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

## Backend

**Not applicable.** No service, feature slice, command/query, handler, validator, DTO, route,
`DbContext` configuration, EF Core migration, or integration event is touched by this ticket.

## Frontend

- **Components:** none. No `.ts` / `.tsx` source change is anticipated.
- **API client:** unchanged.
- **State:** unchanged.
- **Types:** unchanged.
- **Manifest:** `react-router-dom` `^7.13.0` → `^7.18.2`; `vite` moved off the `^8.0.0-beta.13`
  prerelease pin onto stable `^8.2.1`, and the now-redundant `pnpm.overrides` block removed.
- **Lockfile:** refreshed so in-range transitive resolutions pick up their published patches.

### Exposure triage

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
(`` `${ROUTES.ORDERS}/${order.id}` ``), so no user-controlled target exists. The bump removes the
footgun for future code rather than closing an active hole.

### Resolution evidence

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

## Touchpoints

- `src/UI/mango-ui/package.json`
- `src/UI/mango-ui/pnpm-lock.yaml`

## Risks and open questions

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
