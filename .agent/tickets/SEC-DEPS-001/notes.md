# SEC-DEPS-001 — Notes

> State lives in `ticket.json`. Do not restate status here and do not use `- [ ]` checkboxes —
> this file has no authority over progress, and the board script warns if it tries to claim any.

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
`__manifest` or single-fetch endpoint. The turbo-stream RCE, `__manifest` DoS, single-fetch DoS, RSC
CSRF/XSS, SSR hydration injection, prerendered `Location` XSS and document-request CSRF all require a
React Router *server* that does not exist here. Only the backslash open-redirect in `<Link>` /
`useNavigate` touches live code — and even that is inert today because every navigation target is a
hardcoded constant from `src/constants/routes.ts` or an interpolated id. Severity labels on a
Dependabot alert describe the *package*, not this application's exposure.

**Two independent major lines needed patching for the same CVE class.** `minimatch` and
`brace-expansion` each appear twice in the tree at different majors (3.x via eslint, 9.x via
`@typescript-eslint/typescript-estree`; 1.x and 5.x respectively), with *separate* advisories and
*separate* fix floors. Checking only the newest copy would have left the other vulnerable.

## Open Questions

- Should `pnpm audit --audit-level=high` gate CI in `.github/workflows/build.yml`? Recommended as a
  separate follow-up ticket so this backlog cannot silently re-accumulate.

## Blockers

*None.*

## Session Log

### 2026-08-12

Ran the `analyze-requirement` workflow against a paste of 25 Dependabot alerts. Established via
`pnpm audit --json`, `pnpm why` and `npm view` that the real count is 38 advisories over 9 packages,
and that every fix except `react-router-dom` is reachable without touching `package.json`. Triaged
react-router exposure against actual SPA usage. Wrote `plan.md`, presented five clarifying questions,
and the user approved the recommended minimal scope.

Executed: bumped `react-router-dom` to `^7.18.2`, moved `vite` to stable `^8.2.1`, deleted the
`pnpm.overrides` block, and ran `pnpm update`. `pnpm audit` went from 38 advisories to **"No known
vulnerabilities found"**. `pnpm update` also rewrote the manifest range floors to the resolved
minors (axios 1.19.0, react 19.2.8, oidc-client-ts 3.5.0, react-hook-form 7.85.0, typescript-eslint
8.67.0 and others) — wider than the blueprint anticipated, but no major boundary was crossed.

First lint run failed on 6 errors from the new `eslint-plugin-react-hooks` 7.1.1 rules; pinned the
plugin to `~7.0.1` per the scope decision above. Final state: `audit` clean, `lint` exit 0, `build`
green on vite 8.2.1 producing a byte-identical bundle hash (`index-kgYFktrP.js`), and
`git diff --stat` limited to `package.json` + `pnpm-lock.yaml` with zero source files touched.
Manual route pass and the commit are still outstanding.
