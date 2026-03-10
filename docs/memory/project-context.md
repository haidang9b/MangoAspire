# Project Context — MangoAspire

> This file is the living architectural memory of the MangoAspire project.
> Each completed ticket appends its key decisions, conventions, and gotchas here.

---

## UI Layer Conventions (added: 2026-03-10)

### Design System
- **Theme:** Emerald Calm — primary `#10B981`, accent `#F59E0B`, neutrals from `--ec-gray-*`
- **Font:** Montserrat via Google Fonts CDN (`display=swap`) — loaded independently in each app
- **Bootstrap:** 5.3.3 (CDN) — used for grid/utilities only; all component styling is custom BEM
- **Icons:** Font Awesome 6.5.0 (CDN)

### CSS Architecture
- Each app (`Identity.API`, `Mango.Web`) has its own `wwwroot/css/site.css`
- **No shared CSS** between services — same token names, separate files
- All CSS custom properties defined under `:root` and overridden in `[data-bs-theme="dark"]`
- BEM prefix: `.mango-*` for all UI blocks to avoid collision with Bootstrap classes
- Key BEM blocks: `.mango-nav`, `.mango-card`, `.mango-table`, `.form-card`, `.auth-card`, `.mango-btn`, `.mango-badge`, `.mango-alert`, `.mango-pagination`, `.mango-hero`, `.mango-footer`

### Dark Mode
- Stored in `localStorage('mango-theme')`
- Applied via `data-bs-theme` attribute on `<html>` element (Bootstrap 5.3 native dark mode)
- Toggle script runs **inline before body paint** (in `_Layout.cshtml`) to prevent flash of wrong theme

### Navbar
- Sticky via `position: sticky; top: 0; z-index: 1030`
- 3px emerald border on bottom: `border-bottom: 3px solid var(--ec-primary)`

---

## Identity.API View Conventions (added: 2026-03-10)

### ViewModel Namespaces
- **Always** use `Identity.API.MainModule.Account.*` and `Identity.API.MainModule.Home.*`
- **Never** reference `Duende.IdentityServer.Models.*` directly in Views — the project wraps them
- Auth pages use `@using Identity.API.MainModule.Account` at the top of each view

### Auth Pages
- Login and Register use `.auth-card` centered card layout (max-width 480–520px)
- `ViewBag.message` for role SelectList **must be null-guarded**:
  ```csharp
  asp-items="@(ViewBag.message != null ? new SelectList(ViewBag.message) : new SelectList(Enumerable.Empty<string>()))"
  ```

---

## Mango.Web View Conventions (added: 2026-03-10)

### `datetime-local` Input + ASP.NET Model Binding
- **Problem:** `asp-for` on `type="datetime-local"` renders the value in locale format (`"10/03/2026 19:00:00"`) which the browser ignores — time portion is lost
- **Fix:** Use `name=` attribute + explicit `.ToString("yyyy-MM-ddTHH:mm")` for value:
  ```html
  <input type="datetime-local"
         name="CartHeader.PickupDateTime"
         value="@(model.PickupDateTime > DateTime.MinValue ? model.PickupDateTime.ToString("yyyy-MM-ddTHH:mm") : "")" />
  ```
- Always pair with server-side future-date validation in the controller — do **not** rely on client `min` only

### XSS — Product Descriptions
- Product descriptions may contain HTML from admin input
- Use `HtmlEncoder` + strip-tags pattern instead of `Html.Raw`:
  ```csharp
  @HtmlEncoder.Default.Encode(
      System.Text.RegularExpressions.Regex.Replace(description ?? "", "<[^>]+>", " "))
  ```

### CSS `line-clamp`
- Always include both prefixed and standard properties:
  ```css
  -webkit-line-clamp: 3;
  line-clamp: 3;
  ```

---

## Cart & Checkout (added: 2026-03-10)

- `CartController.Checkout [POST]` validates `PickupDateTime > DateTime.Now` before API call
- On validation failure: reloads fresh cart from API, preserves user-entered PickupDateTime, returns `View(freshCart)`
- Coupon discount is recalculated in `LoadCartDtoBasedOnLoggedInUser()` — not stored in cart header from client

---

## Tickets Completed

| Ticket | Title | Date |
|---|---|---|
| UI-REDESIGN-001 | Emerald Calm UI Redesign — Identity.API & Mango.Web | 2026-03-10 |
