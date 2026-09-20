---
id: I18N-REACT
kind: feature
status: completed
priority: normal
createdUtc: 2026-03-10T00:00:00Z
updatedUtc: 2026-03-10T00:00:00Z
completedUtc: 2026-03-10T00:00:00Z
tags: [frontend, spa, i18n]
---

# I18N-REACT — Internationalization (EN/VI) for the mango-ui React SPA

## Plan

### Requirement
Add English/Vietnamese localization across the mango-ui React SPA.

### Approach
i18next / react-i18next, with English and Vietnamese resource bundles. Localization was applied
component-by-component via `useTranslation` rather than through a wrapper or HOC, keeping each
component's strings local to it.

## Steps
- [x] **Step 1 — Business analysis and technical planning**
  - [x] Analyse the requirement
  - [x] Context search across UI components and the main flow
  - [x] Write the technical blueprint
  - [x] Blueprint approved by the user
- [x] **Step 2 — Component and page localization**
  - [x] Shared components: Navbar, ProductCard, SearchBox, Pagination, ChatPopup
  - [x] Store pages: Products, ProductDetails, Cart, Checkout, Confirmation
  - [x] Order pages: Orders, OrderDetails
  - [x] Admin pages: AdminProducts, ProductFormModal, DeleteDialog
- [x] **Step 3 — Verification and quality**
  - [x] TypeScript build passes
  - [x] Lint passes
  - [x] Language switching tested manually in the dev server
- [x] **Step 4 — Documentation, memory and completion**
  - [x] i18n behaviour documented
  - [x] Conventional commit created
  - [x] Findings promoted into project memory

## Links
- Branch:
- Commits:
- PRs:
- Files: src/UI/mango-ui/src
- Docs: docs/frontend/i18n.md
- Memory: .agent/memory/domains/frontend-spa.md

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
