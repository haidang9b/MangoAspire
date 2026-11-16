---
applyTo: "src/UI/mango-ui/src/**/*.ts,src/UI/mango-ui/src/**/*.tsx"
---

<!-- GENERATED from .claude/ by scripts/sync-agent-harness.ps1 - do not edit directly -->

# React SPA standards (`src/UI/mango-ui`)

- **Stack:** React 19 + Vite + TypeScript (strict), react-router for routing, react-hook-form for forms, axios for HTTP.
- **Types:** never `any`. Use `import type` for type-only imports. Define an interface for every API response, in `src/UI/mango-ui/src/types/` or next to the client call.
- **Imports:** prefer the `@/` alias (configured in `vite.config.ts` and `tsconfig`).
- **API calls:** live in `src/UI/mango-ui/src/api/` only. Components consume them through hooks in `src/UI/mango-ui/src/hooks/` (e.g. `useFetch`), never by calling axios directly.
- **State:**
  - Reuse the existing contexts in `src/UI/mango-ui/src/context/` (`AuthContext`, cart, theme, notifications) before adding any global state.
  - Redux and other state libraries were deliberately not adopted.
- **i18n:** every user-facing string goes through react-i18next, with EN and VI resources in `src/UI/mango-ui/src/locales/`.
- **Package manager:** pnpm only, from the repo root with `--dir src/UI/mango-ui`.
- **Verify:** `pnpm --dir src/UI/mango-ui lint` and `pnpm --dir src/UI/mango-ui build`. There is no test runner; never invent test commands.
- **Background** (version holds, audit residuals): `.agent/memory/domains/frontend-spa.md`.
