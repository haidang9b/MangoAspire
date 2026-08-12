# Project Memory Index

Descriptive memory — what is *true* about this project and what we learned the hard way.
Normative rules ("what you must do") live in `.agent/rules/`, `.agent/CODING_CONVENTIONS.md`,
`.agent/API_PROJECT_STRUCTURE.md` and `AGENTS.md`.

Read this index, then read only the file(s) covering the area you are touching. Paths are plain
links on purpose — do **not** turn them into `@`-imports (see `.agent/memory/MEMORY_GUIDE.md`).

## Always useful

- `.agent/memory/architecture.md` — source map of `src/`, project-wide invariants, and the
  deliberate exceptions (the `Mango.Orchestrators` saga repository, the legacy `Coupon.API`).
- `.agent/memory/stack.md` — exact frameworks and versions, package-manager rules, central package
  management.

## Read when touching…

- Identity, auth middleware, the Duende/OpenIddict provider switch, AppHost wiring →
  `.agent/memory/domains/identity.md`
- Checkout across services, saga state, compensation, `CorrelationId` →
  `.agent/memory/domains/checkout-saga.md`
- Razor views in `Identity.API` or `Mango.Web`, Emerald Calm CSS, dark mode →
  `.agent/memory/domains/ui-web.md`
- `Mango.Web` cart/checkout controller, coupons, `PickupDateTime` →
  `.agent/memory/domains/cart-checkout.md`
- The React SPA `src/UI/mango-ui` — pnpm, i18n, contexts, the `@/` alias →
  `.agent/memory/domains/frontend-spa.md`

## Doing, not knowing

- Build / test / lint command reference → `.agent/memory/ops/commands.md`
- "Am I done?" checklist per change type → `.agent/memory/ops/task-completion.md`
- Serena MCP, dev scripts, harness generation → `.agent/tools/`
- How to write and prune memory → `.agent/memory/MEMORY_GUIDE.md`

## Current and past work

Ticket state lives in `.agent/tickets/<TICKET-ID>/ticket.json`; the rendered board is
`.agent/ui/board.html` (`pwsh ./scripts/update-ticket-board.ps1`).
