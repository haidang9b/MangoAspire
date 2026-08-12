# UI-REDESIGN-001 — Notes

> State lives in `ticket.json`. Migrated from the retired `docs/tracking/UI-REDESIGN-001.md`.

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
