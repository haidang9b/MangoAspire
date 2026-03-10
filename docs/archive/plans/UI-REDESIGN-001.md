# Ticket Tracker: UI-REDESIGN-001

**Title**: Emerald Calm UI Redesign — Identity.API & Mango.Web
**Current Status**: Completed
**Last Updated**: 2026-03-10 19:52

---

## Step 1: Business Analysis and Technical Planning
- [x] Run `analyze-requirement` sub-workflow.
- [x] Perform Context Search (Identity.API & Mango.Web views/CSS).
- [x] Generate Technical Blueprint at `docs/plans/TICKET-ID.md`.
- [x] Present Clarifying Questions to the user (theme, font, dark mode, layout options).
- [x] **PAUSE**: Waited for User Approval.
- [x] Blueprint Approved — User chose **Option C: Emerald Calm + Montserrat**.

## Step 2: Implementation (Full-Stack)
- [x] **Identity.API CSS**: Full BEM design system, CSS custom properties, dark/light mode tokens.
- [x] **Identity.API Layout**: Bootstrap 5.3 CDN, Montserrat CDN, Font Awesome 6, dark toggle JS.
- [x] **Identity.API Nav**: `.mango-nav` sticky navbar, hamburger, user display, dark toggle.
- [x] **Identity.API Auth Pages**: Login, Register → `.auth-card` centered; LoggedOut, Error → themed cards.
- [x] **Mango.Web CSS**: Same Emerald Calm tokens, separate file, full BEM block set.
- [x] **Mango.Web Layout**: Bootstrap 5.3, Montserrat, sticky nav, TempData alerts, dark toggle.
- [x] **Mango.Web LoginPartial**: BEM nav links with icons.
- [x] **Home/Index**: Hero + product card grid + BEM pagination.
- [x] **Home/Details**: Product detail card, image, badges, quantity, Add-to-Cart (disabled OOS).
- [x] **Product/Index**: `.mango-table`, filter bar, stock badges, icon actions, pagination.
- [x] **Product/Create & Edit**: `.form-card`, two-column field groups.
- [x] **Product/Delete**: Danger-themed confirmation card.
- [x] **Cart/Index**: `.mango-table` + coupon/totals + Checkout CTA.
- [x] **Cart/Checkout**: Two-column — form-card + order summary. Fixed `datetime-local` input.
- [x] **Order/Index**: `.mango-table` + status badges + pagination.
- [x] **Order/Details**: Info cards (Order + Customer) + items table with emerald total row.

## Step 3: Verification and Quality
- [x] `dotnet build Identity.API` → ✅ Build succeeded.
- [x] `dotnet build Mango.Web` → ✅ Build succeeded.
- [x] **Bug Fix**: `LoggedOut.cshtml` wrong model namespace → `Identity.API.MainModule.Account`.
- [x] **Bug Fix**: `Error.cshtml` wrong Duende namespace → `Identity.API.MainModule.Home`.
- [x] **Bug Fix**: `Register.cshtml` `ViewBag.message` null → null guard on `SelectList`.
- [x] **Bug Fix**: Checkout `datetime-local` time broken → manual ISO `yyyy-MM-ddTHH:mm` value.
- [ ] **Refactor**: Run `fix-warnings` for C# 14 warnings (deferred).

## Step 4: Documentation, Memory, and Completion
- [x] **Docs**: `docs/plans/TICKET-ID.md` — full Blueprint written.
- [x] **Code Review**: `brain/.../code_review.md` — completed.
- [ ] **Commit**: Run `generate-commit` for Conventional Commit message.
- [ ] **Memory**: Append findings to `docs/memory/project-context.md`.
- [ ] **Archive**: Move tracker + blueprint to `docs/archive/plans/`.

---

## Active Blockers / Notes
- [x] **Code Review fixes** (2026-03-10):
  - `CartController.Checkout` — added `PickupDateTime <= DateTime.Now` server-side guard ✅
  - `Home/Index.cshtml` + `Home/Details.cshtml` — replaced `Html.Raw` with `HtmlEncoder` + strip-tags ✅
  - `Mango.Web/wwwroot/css/site.css` — added `line-clamp: 3` (standard) alongside `-webkit-line-clamp` ✅

## Memory Scratchpad
- **BEM prefix**: `.mango-*` for all app blocks to avoid Bootstrap collision.
- **Dark mode**: `localStorage('mango-theme')` → `data-bs-theme` on `<html>`. Applied before first paint to avoid flash of wrong theme.
- **Separate CSS**: Identity.API and Mango.Web share CSS token names but each file is fully self-contained.
- **Auth namespaces**: Always use `Identity.API.MainModule.Account/Home` — not Duende's built-in model classes.
- **`datetime-local` + `asp-for`**: Tag helper renders locale format, not ISO. Use `name=` + `.ToString("yyyy-MM-ddTHH:mm")` for value instead.
