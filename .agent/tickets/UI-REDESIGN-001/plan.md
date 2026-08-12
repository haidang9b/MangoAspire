# Plan: UI Redesign — Identity Server & Mango.Web

**Ticket ID:** UI-REDESIGN-001  
**Created:** 2026-03-10  
**Status:** In Progress — Step 2 Implementation  
**Theme:** Emerald Calm (Option C) | Font: Montserrat | Bootstrap 5.3

---

## Background

Both `Identity.API` (Duende IdentityServer) and `Mango.Web` (ASP.NET MVC) previously used bare Bootstrap 5 with almost no custom CSS, no visual hierarchy, no brand tokens, and no BEM structure.

## Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Theme | Emerald Calm (Option C) | User selected |
| Primary color | `#10B981` (Emerald-500) | Brand warmth + readability |
| Accent color | `#F59E0B` (Amber-400) | Mango brand, price highlights |
| Font | Montserrat (Google Fonts CDN) | User selected; loaded independently per app |
| Dark mode | Toggle switch (localStorage) | User requested |
| Navbar | Sticky | User requested |
| CSS strategy | Separate `site.css` per app, same tokens | User: no shared codebase |
| BEM scope | All layout wrappers + components | User requested |
| Product listing | Table | User requested |
| Auth layout | Centered card (modern SSO) | Replaces old side-by-side |

---

## Files Changed

### Identity.API
| File | Change |
|---|---|
| `wwwroot/css/site.css` | Full rewrite — BEM design system, CSS tokens, dark mode |
| `Views/Shared/_Layout.cshtml` | Bootstrap 5.3, Montserrat CDN, dark mode toggle JS |
| `Views/Shared/_Nav.cshtml` | BEM `.mango-nav`, user display, hamburger, theme toggle |
| `Views/Account/Login.cshtml` | `.auth-card` centered layout, `.auth-form` BEM |
| `Views/Account/Register.cshtml` | `.auth-card`, 2-col name row, `.auth-form` BEM |
| `Views/Account/LoggedOut.cshtml` | Styled confirmation card |
| `Views/Shared/Error.cshtml` | Styled error card |

### Mango.Web
| File | Change |
|---|---|
| `wwwroot/css/site.css` | Full rewrite — same theme BEM system, separate file |
| `Views/Shared/_Layout.cshtml` | Bootstrap 5.3, Montserrat CDN, BEM page wrapper, alerts, footer |
| `Views/Shared/_LoginPartial.cshtml` | BEM nav links for user/auth |
| `Views/Home/Index.cshtml` | Hero + product card grid |
| `Views/Product/ProductIndex.cshtml` | `.mango-table`, filter bar, pagination |
| `Views/Product/ProductCreate.cshtml` | `.form-card` layout |
| `Views/Product/ProductEdit.cshtml` | `.form-card` layout |
| `Views/Product/ProductDelete.cshtml` | Confirmation card |
| `Views/Cart/Index.cshtml` | `.mango-table` styled cart |
| `Views/Order/Index.cshtml` | `.mango-table` order history |
| `Views/Order/Details.cshtml` | Order detail card |

---

## BEM Blocks Defined

| Block | Description |
|---|---|
| `.mango-page` | Root flex-column page wrapper |
| `.mango-page__body` | Main content grow area |
| `.mango-container` | Max-width centered container |
| `.mango-nav` | Sticky top navigation |
| `.mango-page-header` | Page title + action button row |
| `.mango-card` | Generic surface card |
| `.mango-product-card` | Product listing card (Home) |
| `.mango-table-wrapper` | Rounded table container |
| `.mango-table` | Styled table with emerald headers |
| `.form-card` | Create/Edit form container |
| `.mango-form__*` | Form group, label, input, select, textarea |
| `.auth-card` | Auth page centered card (Identity) |
| `.auth-form__*` | Auth form inputs, labels, actions |
| `.mango-btn` | Button with modifiers: primary, secondary, danger, accent, sm, block |
| `.mango-badge` | Status badge: success, danger, warning, info |
| `.mango-alert` | Alert bar with left border: success, danger, warning, info |
| `.mango-pagination` | Pagination info + page links |
| `.mango-filter` | Table filter bar |
| `.mango-hero` | Home page hero section |
| `.mango-footer` | App footer |

---

## Verification Plan

1. Run `Identity.API` → check Login card, Register card, Nav, dark toggle
2. Run `Mango.Web` → check Navbar, Home hero, Product table, Cart, Orders
3. Test mobile breakpoint (< 768px) — hamburger menu opens
4. Toggle dark mode on both apps — all tokens respond correctly
