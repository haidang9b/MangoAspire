---
id: UI-REDESIGN-001
kind: feature
status: completed
priority: normal
createdUtc: 2026-03-10T00:00:00Z
updatedUtc: 2026-03-10T19:55:00Z
completedUtc: 2026-03-10T19:55:00Z
tags: [frontend, razor, design-system]
---

# UI-REDESIGN-001 — Emerald Calm UI redesign — Identity.API and Mango.Web

## Plan

### Requirement
Redesign `Identity.API` and `Mango.Web` with a shared visual language (theme, typography, dark
mode) without introducing a shared stylesheet.

### Approach
- **Option C — Emerald Calm + Montserrat** was chosen by the user from four presented themes.
- **Separate CSS per app.** `Identity.API` and `Mango.Web` share token *names* but each
  `wwwroot/css/site.css` is fully self-contained. No shared stylesheet was introduced.
- **BEM with a `.mango-*` prefix** on every block, so custom components cannot collide with
  Bootstrap's own classes.
- Bootstrap 5.3 is used for grid and utilities only; every component is styled by hand.

## Steps
- [x] **Step 1 — Business analysis and technical planning**
  - [x] Run the analyze-requirement sub-workflow
  - [x] Context search across Identity.API and Mango.Web views/CSS
  - [x] Write the technical blueprint
  - [x] Present theme/font/dark-mode/layout options to the user
  - [x] Blueprint approved — Option C: Emerald Calm + Montserrat
- [x] **Step 2 — Implementation (full-stack)**
  - [x] Identity.API: BEM design system, custom properties, dark/light tokens
  - [x] Identity.API: layout, sticky nav, auth pages on .auth-card
  - [x] Mango.Web: same tokens in a separate file, full BEM block set
  - [x] Mango.Web: layout, LoginPartial, TempData alerts, dark toggle
  - [x] Home, Product, Cart and Order views restyled
- [x] **Step 3 — Verification and quality**
  - [x] dotnet build Identity.API and Mango.Web succeed
  - [x] Fix 4 view bugs found during verification
  - [x] Apply code-review fixes (checkout guard, XSS, line-clamp)
  - [x] ~~Run fix-warnings for C# 14 compiler warnings~~ — skipped: deferred at completion
- [x] **Step 4 — Documentation, memory and completion**
  - [x] Blueprint written above
  - [x] Conventional commit created
  - [x] Findings promoted into project memory

## Links
- Branch:
- Commits:
- PRs:
- Files: src/Services/Identity.API/wwwroot/css/site.css, src/Web/Mango.Web/wwwroot/css/site.css
- Docs:
- Memory: .agent/memory/domains/ui-web.md, .agent/memory/domains/cart-checkout.md

## Decisions
- **Option C — Emerald Calm + Montserrat** was chosen by the user from four presented themes.
- **Separate CSS per app.** `Identity.API` and `Mango.Web` share token *names* but each
  `wwwroot/css/site.css` is fully self-contained. No shared stylesheet was introduced.
- **BEM with a `.mango-*` prefix** on every block, so custom components cannot collide with
  Bootstrap's own classes.
- Bootstrap 5.3 is used for grid and utilities only; every component is styled by hand.

## Gotchas
- **`datetime-local` + `asp-for`** renders a locale-formatted value the browser silently ignores,
  so the time portion is lost. Fixed with `name=` plus an explicit `yyyy-MM-ddTHH:mm` value.
- **Dark mode must be applied before first paint** or the page flashes the wrong theme. The toggle
  script is inline in `_Layout.cshtml`, ahead of the body.
- **Duende model namespaces leak into views.** `LoggedOut.cshtml` and `Error.cshtml` had been
  pointed at `Duende.IdentityServer.Models.*`; the project wraps those, so views must use
  `Identity.API.MainModule.Account` / `.Home`.
- **`ViewBag.message` can be null** on `Register.cshtml`, which throws inside `new SelectList(...)`.
  It needs an explicit null guard.

Both are now recorded in `.agent/memory/domains/ui-web.md`.

## Open Questions
None.

## Blockers
None were opened.

## Session Log

### 2026-03-10
Full redesign delivered across both Razor apps: CSS design system, layouts, navs, auth pages, and
the Home / Product / Cart / Order view sets. Builds green for both projects.

A code review raised three follow-ups, all fixed the same day: a server-side
`PickupDateTime <= DateTime.Now` guard in `CartController.Checkout`, `Html.Raw` replaced with
`HtmlEncoder` + strip-tags on the Home views, and the standard `line-clamp` property added
alongside `-webkit-line-clamp`.

`fix-warnings` was deliberately deferred rather than run — the only piece of the ticket not
completed.
