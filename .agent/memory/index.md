# Project Memory Index

Descriptive memory — what is *true* about this project and what we learned the hard way.
Normative rules ("what you must do") live in `.claude/rules/` (path-scoped) and `AGENTS.md`.

Read this index, then read only the file(s) covering the area you are touching. Nothing imports this file; `AGENTS.md` links it.
Domain files also surface automatically through `.claude/rules/memory/`.

## Always useful

- `.agent/memory/architecture.md` — source map of `src/`, project-wide invariants, and the
  deliberate exception (the `Mango.Orchestrators` saga repository).
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
- The React SPA `src/UI/mango-ui` — pnpm, i18n, contexts, the `@/` alias, deliberate dependency
  version holds and the accepted audit residual →
  `.agent/memory/domains/frontend-spa.md`

## Doing, not knowing

- Build / test / lint command reference → `.agent/memory/ops/commands.md`
- "Am I done?" checklist per change type → `.agent/memory/ops/task-completion.md`
- Serena MCP, dev scripts, harness generation → `.agent/tools/`
- How to write and prune memory → `.agent/memory/MEMORY_GUIDE.md`

## Current and past work

Ticket state lives in `.agent/tickets/<TICKET-ID>/ticket.md` — one file per ticket, `status` in its
front-matter.
