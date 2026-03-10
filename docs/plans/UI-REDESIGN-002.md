# UI-REDESIGN-002: Migrate `useFetch` → TanStack Query

## Background

The current `useFetch` hook in `src/hooks/useFetch.ts` is a hand-rolled, module-level cache with stale-while-revalidate behaviour. While it works, it lacks:
- DevTools support (React Query DevTools)
- Automatic background refetching / window focus refetching
- First-class mutation + cache invalidation primitives
- Fine-grained loading/error states (`isPending`, `isFetching`)
- Retry logic

This ticket adds TanStack Query v5 (`@tanstack/react-query`) alongside `useFetch` (which is **kept, not deleted**) and migrates all 5 affected pages and 1 domain hook to `useQuery`.

> **No backend changes are required** — this is a pure frontend refactor.

---

## Proposed Changes

### 1. Infrastructure / Bootstrap

#### [MODIFY] package.json
- Add `@tanstack/react-query` (v5) and `@tanstack/react-query-devtools` as dependencies.
- Command: `pnpm add @tanstack/react-query @tanstack/react-query-devtools`

#### [NEW] `src/lib/queryClient.ts`
- Singleton `QueryClient` with:
  - `staleTime: 60_000` (matches current 1-min TTL)
  - `retry: 1`
  - `refetchOnWindowFocus: false`

#### [MODIFY] `src/main.tsx`
- Wrap `<App />` with `<QueryClientProvider client={queryClient}>`.
- Add `<ReactQueryDevtools initialIsOpen={false} />` (dev-only).

#### [MODIFY] `src/constants/cacheKeys.ts`
- Convert plain strings → tuple factory functions:
  ```ts
  export const QUERY_KEYS = {
    products: (filters) => ['products', filters] as const,
    catalogTypes: () => ['catalog-types'] as const,
    productDetail: (id) => ['products', id] as const,
    orders: (filters) => ['orders-list', filters] as const,
    orderDetail: (id) => ['order-detail', id] as const,
  };
  ```

---

### 2. Hook Refactor

#### [MODIFY] `src/hooks/useProducts.ts`
- Replace `useFetch` with `useQuery`.
- `queryKey`: `QUERY_KEYS.products({ pageIndex, pageSize, catalogTypeId, search })`
- Return `{ products, totalCount, isLoading: isPending, error, refetch }`.

#### [KEEP] `src/hooks/useFetch.ts`
- **Not deleted.** Kept as-is for potential future use. Pages will be migrated to TanStack Query but `useFetch` remains exported and available.

---

### 3. Page Refactors

Each page replaces `useFetch(cacheKey, fetcher, opts)` with `useQuery({ queryKey, queryFn, enabled? })`.

#### [MODIFY] `ProductsPage.tsx`
- `catalogTypes` fetch → `useQuery({ queryKey: QUERY_KEYS.catalogTypes(), queryFn })`.

#### [MODIFY] `ProductDetailsPage.tsx`
- Product detail fetch → `useQuery({ queryKey: QUERY_KEYS.productDetail(id), queryFn, enabled: !!id })`.

#### [MODIFY] `AdminProductsPage.tsx`
- Products list + catalog types → `useQuery(...)`.
- **Key change:** Replace `tick` state + `reload()` hack with `useQueryClient().invalidateQueries(...)` after save/delete.

#### [MODIFY] `OrdersPage.tsx`
- Orders list fetch → `useQuery({ queryKey: QUERY_KEYS.orders({ pageIndex, pageSize }), queryFn })`.

#### [MODIFY] `OrderDetailsPage.tsx`
- Order detail fetch → `useQuery({ queryKey: QUERY_KEYS.orderDetail(id), queryFn, enabled: !!id })`.

---

## User Review Required

> [!IMPORTANT]
> `CACHE_KEYS` will be replaced by `QUERY_KEYS`. All consumers are only the files listed above. Confirm if you want `CACHE_KEYS` kept as a deprecated alias or removed.

> [!NOTE]
> `refetchOnWindowFocus` is set to `false` to match existing behaviour. Can be changed to `true` for a better UX.

> [!NOTE]
> React Query DevTools adds a floating dev panel — dev mode only, excluded from production builds.

---

## Verification Plan

### Automated Tests
- **File:** `src/hooks/useProducts.test.ts`
- **Run:** `pnpm test`

### Manual Browser Smoke Tests

| # | Page | What to verify |
|---|------|---------------|
| 1 | `/` (ProductsPage) | Grid loads, filter chips work, pagination works |
| 2 | `/products/:id` | Product details load |
| 3 | `/admin/products` | List loads; create/edit/delete → list auto-refreshes |
| 4 | `/orders` | Orders list + pagination |
| 5 | `/orders/:id` | Order detail loads |
| 6 | Network DevTools | No duplicate API calls |
| 7 | React Query DevTools | Cache entries appear with correct keys |
