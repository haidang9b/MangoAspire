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
