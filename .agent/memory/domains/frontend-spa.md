# Frontend SPA — `src/UI/mango-ui`

React 19 + Vite + TypeScript. See `.agent/memory/stack.md` for versions.

## Key facts

- Package manager is **pnpm** (`pnpm-lock.yaml`). Run from the repo root with
  `--dir src/UI/mango-ui` — see `.agent/memory/ops/commands.md`.
- TypeScript **strict**, no `any`. ESLint config: `eslint.config.js`.
- Path alias `@/` is configured in `vite.config.ts` and `tsconfig`. Prefer alias imports and
  `import type` for type-only imports.
- **No test runner is configured.** Verify with `pnpm lint` + `pnpm build` only — do not invent test
  commands.
- API client code lives in `src/api/` and is consumed through hooks and components.
- Reuse the existing contexts before adding a global: `AuthContext`, plus the cart, theme and
  notification patterns.
- Localization is i18next / react-i18next, EN + VI (`docs/frontend/i18n.md`). State management is
  Context API + custom hooks — Redux was deliberately not adopted
  (`docs/frontend/state-management.md`).
- Docker: `Dockerfile` + `nginx.conf` for containerized serving. The module carries its own
  `ARCHITECTURE.md` and `README.md`.

## Deliberate version holds (`~` ranges in `package.json`)

`pnpm update` re-resolves everything in range **and rewrites manifest floors**, so these holds are
what keep a security refresh from also shipping unrelated upgrades:

- `eslint-plugin-react-hooks ~7.0.1` — 7.1.x adds `purity` / `refs` / `set-state-in-effect`
  rules that fail lint on 6 existing sites (SEC-DEPS-001).
- `react`, `react-dom`, `@types/react`, `@types/react-dom ~19.2.x` — React 19.3 grows the JS
  bundle ~29 kB, past Vite's 500 kB chunk warning (main chunk is ~494 kB on 19.2) (SEC-DEPS-002).

`pnpm add pkg@~x.y.z` saves the range with `^` (default save-prefix) — set `~` by hand, then
`pnpm install`.

## Accepted audit residual

`minimatch` 10.x (GHSA-g3hv-75wg-m249, moderate, lint-time only via typescript-eslint): fixed only
in 11.0.1, outside typescript-eslint's `^10.2.2` range. Not overridden; recheck when
typescript-eslint widens the range.
