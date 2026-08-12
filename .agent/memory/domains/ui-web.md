# Razor UI — `Identity.API` and `Mango.Web`

The MVC/Razor side of the product. The React SPA is separate — see
`.agent/memory/domains/frontend-spa.md`.

## Emerald Calm design system

- Primary `#10B981`, accent `#F59E0B`, neutrals from `--ec-gray-*`.
- Font: Montserrat via the Google Fonts CDN (`display=swap`), loaded independently in each app.
- Bootstrap 5.3.3 (CDN) for grid and utilities only — all component styling is custom BEM.
- Icons: Font Awesome 6.5.0 (CDN).

## CSS architecture

- Each app (`Identity.API`, `Mango.Web`) has its own `wwwroot/css/site.css`.
- **No shared CSS between services** — the same token names, deliberately separate files.
- All custom properties are defined under `:root` and overridden in `[data-bs-theme="dark"]`.
- BEM prefix `.mango-*` on every block, to avoid collisions with Bootstrap classes. Key blocks:
  `.mango-nav`, `.mango-card`, `.mango-table`, `.form-card`, `.auth-card`, `.mango-btn`,
  `.mango-badge`, `.mango-alert`, `.mango-pagination`, `.mango-hero`, `.mango-footer`.

## Dark mode

Stored in `localStorage('mango-theme')` and applied via the `data-bs-theme` attribute on `<html>`
(Bootstrap 5.3 native dark mode). **The toggle script runs inline before body paint** (in
`_Layout.cshtml`) to prevent a flash of the wrong theme. Keep it inline and keep it first.

## Navbar

Sticky via `position: sticky; top: 0; z-index: 1030`, with a 3px emerald bottom border
(`border-bottom: 3px solid var(--ec-primary)`).

## `Identity.API` view conventions

- **Always** use the `Identity.API.MainModule.Account.*` and `Identity.API.MainModule.Home.*`
  ViewModel namespaces. **Never** reference `Duende.IdentityServer.Models.*` directly in a view —
  the project wraps them. Auth pages put `@using Identity.API.MainModule.Account` at the top.
- Login and Register use the `.auth-card` centered layout (max-width 480–520px).
- The `ViewBag.message` role `SelectList` **must be null-guarded**:
  ```csharp
  asp-items="@(ViewBag.message != null ? new SelectList(ViewBag.message) : new SelectList(Enumerable.Empty<string>()))"
  ```

## `Mango.Web` view gotchas

### `datetime-local` input + ASP.NET model binding

`asp-for` on `type="datetime-local"` renders the value in locale format
(`"10/03/2026 19:00:00"`), which the browser ignores — **the time portion is silently lost**. Use a
plain `name=` attribute plus an explicit ISO value:

```html
<input type="datetime-local"
       name="CartHeader.PickupDateTime"
       value="@(model.PickupDateTime > DateTime.MinValue ? model.PickupDateTime.ToString("yyyy-MM-ddTHH:mm") : "")" />
```

Always pair this with server-side future-date validation — never rely on the client `min` alone.

### XSS in product descriptions

Descriptions may contain HTML from admin input. Use `HtmlEncoder` + strip-tags rather than
`Html.Raw`:

```csharp
@HtmlEncoder.Default.Encode(
    System.Text.RegularExpressions.Regex.Replace(description ?? "", "<[^>]+>", " "))
```

### CSS `line-clamp`

Always emit both the prefixed and the standard property:

```css
-webkit-line-clamp: 3;
line-clamp: 3;
```
