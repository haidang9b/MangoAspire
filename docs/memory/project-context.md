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

## React Frontend — Data Fetching (added: 2026-03-10, ticket: UI-REDESIGN-002)

### Library
- **TanStack Query v5** (`@tanstack/react-query`) — replaces custom `useFetch` hook
- `useFetch.ts` is **kept** in `src/hooks/` for potential future use but no longer used by any page

### QueryClient Setup
- Singleton defined in `src/lib/queryClient.ts`
- Defaults: `staleTime: 60_000`, `retry: 1`, `refetchOnWindowFocus: true`
- `QueryClientProvider` wraps `<App />` in `main.tsx`

### Query Key Conventions — `QUERY_KEYS` in `src/constants/cacheKeys.ts`
- All keys are **typed tuple factory functions** (not plain strings)
- Hierarchical structure: base key → derived keys → enables partial invalidation
  ```ts
  productsAll:   ()        => ['products']
  products:      (filters) => [...productsAll(), filters]
  productDetail: (id)      => [...productsAll(), 'detail', id]
  ```
- **Never use magic strings** in `invalidateQueries` — always use `QUERY_KEYS.*`
- Broad invalidation after mutations: `queryClient.invalidateQueries({ queryKey: QUERY_KEYS.productsAll() })`

### Domain Hooks Pattern
- Wrap repeated `useQuery` calls in named domain hooks (same pattern as `useProducts`):
  - `useProducts(filters)` — paginated product list
  - `useCatalogTypes()` — shared between `ProductsPage` and `AdminProductsPage`; TanStack deduplicates the network request automatically
- Return `{ data, isLoading, error, refetch }` — keep surface stable

### `reload` → `refetch`
- All hooks now return `refetch` (not `reload`) to match TanStack Query conventions

### AdminProductsPage — Mutation + Invalidation
- **Old pattern (removed):** `tick` state + manual `reload()` call after save/delete — caused double-fetch
- **New pattern:** `await queryClient.invalidateQueries({ queryKey: QUERY_KEYS.productsAll() })` — automatically refetches all active product queries

### Vite Config Gotcha — pnpm + React
- Add `resolve.dedupe: ['react', 'react-dom']` to `vite.config.ts`
- **Without this**, pnpm's symlink isolation can cause Vite to bundle two React instances → "Invalid hook call" runtime crash

### DevTools Warning
- `@tanstack/react-query-devtools` **cannot be used** with this pnpm setup
- It bundles its own copy of `@tanstack/react-query`, causing a context mismatch ("No QueryClient set")
- Package has been removed from `package.json`

---

## App.tsx Structure (added: 2026-03-10)

`App.tsx` is split into three focused files:
- `src/router/AppRouter.tsx` — all `<Routes>` definitions, grouped by access level (public / protected / admin)
- `src/providers/AppProviders.tsx` — context provider composition: `ThemeProvider → AuthProvider → CartProvider`
- `App.tsx` — slim shell combining `AppProviders + BrowserRouter + AppRouter`

---

## ChatAgent API & ChatPopup (added: 2026-03-10)

### API Response Shape
- `GET /api/chat-histories?pageSize=5&pageIndex=1` — implemented in `ChatRoute.cs`
- Returns **`PaginatedItems<ChatMessageDto>` directly** (`Results.Ok(result)`) — **NOT wrapped in `ResultModel`**
- Use `PagedModel<ChatMessage>` as the Axios response type, **not** `ResultModel<PagedModel<ChatMessage>>`
- `ChatMessageDto` fields are PascalCase from C#: `Id`, `Role` (enum: 0=System, 1=User, 2=Assistant), `Content`, `CreatedAt`
- `PagedModel<T>` has: `pageIndex`, `pageSize`, `count`, `hasNextPage` (authoritative — use directly, don't recompute), `data: T[]`

### `src/types/api.ts` — `PagedModel<T>`
```ts
interface PagedModel<T> { pageIndex; pageSize; count; hasNextPage: boolean; data: T[]; }
type PaginatedItems<T> = PagedModel<T>;  // alias kept for compatibility
```

### `src/api/chat.ts`
- Default `pageSize = 5` (set by user)
- `mapRole()` converts numeric enum (0/1/2) or string → `ChatMessageRole`
- `normalizeMessage()` typed as `ChatMessage → ChatMessage` (no `Record<string,unknown>`)
- `hasMore` in `ChatHistoryPage` = `paged.hasNextPage` (straight from BE)
- `sendMessage()` uses raw `fetch` (not Axios) for SSE/streaming with `IAsyncEnumerable<PromptResponseDto>`

### ChatPopup infinite scroll pattern
- **Scroll up** triggers load of older messages (higher `pageIndex`)
- `IntersectionObserver` watches a `topSentinelRef` div at the top of `.chat-messages`
- Scroll position preserved after prepend: `container.scrollTop = container.scrollHeight - prevScrollHeight`
- State fully resets when popup is closed (`initialLoadDone` ref prevents double-load)
- Error state exposed in UI (`historyError`) — no silent failures
- Typing indicator: `isStreamingPlaceholder = isSending && role === 'Assistant' && content === ''`

---

## Tickets Completed

| Ticket | Title | Date |
|---|---|---|
| UI-REDESIGN-001 | Emerald Calm UI Redesign — Identity.API & Mango.Web | 2026-03-10 |
| UI-REDESIGN-002 | Migrate useFetch → TanStack Query (React frontend) | 2026-03-10 |
