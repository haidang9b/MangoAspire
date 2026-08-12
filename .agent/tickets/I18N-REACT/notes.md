# I18N-REACT — Notes

> State lives in `ticket.json`. Migrated from the retired `docs/archive/tracking/I18N-REACT.md`.

## Decisions

- i18next / react-i18next, with English and Vietnamese resource bundles.
- Localization was applied component-by-component via `useTranslation` rather than through a
  wrapper or HOC, keeping each component's strings local to it.

## Gotchas

- The original tracker recorded verification as `npm run build` / `npm run lint` / `npm run dev`.
  **That is wrong for this repo** — the SPA uses **pnpm** (`pnpm --dir src/UI/mango-ui <cmd>`), and
  there is no test runner at all. The commands were correct in spirit, wrong in tooling.

## Open Questions

None.

## Blockers

None were opened.

## Session Log

### 2026-03-10

Localized the shared components, the store pages, the order pages and the admin pages. Verified via
a TypeScript build, a lint pass, and manual language switching in the dev server. Behaviour is
documented in `docs/frontend/i18n.md`.
