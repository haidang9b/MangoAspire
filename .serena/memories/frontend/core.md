# Frontend Module — mango-ui

Location: `src/UI/mango-ui`. React 19 + Vite + TypeScript SPA. See `mem:tech_stack` and `mem:conventions` (Frontend section).

## Key facts
- Package manager: **pnpm** (`pnpm-lock.yaml`). Run from repo root with `--dir src/UI/mango-ui` (see `mem:suggested_commands`).
- TypeScript **strict**, no `any`. ESLint config: `eslint.config.js`.
- Path alias `@/` configured in `vite.config.ts` + `tsconfig`.
- **No test runner** configured — verify only via `pnpm lint` + `pnpm build`.
- API client code lives in `src/api/`, consumed through hooks/components.
- Reuse existing contexts before adding globals: `AuthContext`, cart/theme/notification patterns.
- Docker: `Dockerfile` + `nginx.conf` present for containerized serving. Module has its own `ARCHITECTURE.md` / `README.md`.
