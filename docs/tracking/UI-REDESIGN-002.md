# Tracker: UI-REDESIGN-002 — Migrate useFetch → TanStack Query

**Status:** 🟡 Step 3 — Awaiting User Smoke Test  
**Started:** 2026-03-10  
**Last updated:** 2026-03-10  

---

## Step 1: Business Analysis & Technical Planning
- [x] Load project memory
- [x] Scan frontend codebase
- [x] Draft implementation plan (`docs/plans/UI-REDESIGN-002.md`)
- [x] User Approved

## Step 2: Implementation ✅
- [x] Installed `@tanstack/react-query` 5.90.21
- [x] Added `QueryClientProvider` to `main.tsx`
- [x] Created `src/lib/queryClient.ts` (staleTime 60s, retry 1, refetchOnWindowFocus true)
- [x] Refactored `useProducts.ts` → `useQuery`
- [x] Refactored `ProductsPage.tsx`
- [x] Refactored `ProductDetailsPage.tsx`
- [x] Refactored `AdminProductsPage.tsx` (tick+reload → invalidateQueries)
- [x] Refactored `OrdersPage.tsx`
- [x] Refactored `OrderDetailsPage.tsx`
- [x] `useFetch.ts` kept for future use
- [x] `CACHE_KEYS` → `QUERY_KEYS` factory functions (+ deprecated alias kept)
- [x] `resolve.dedupe: ['react','react-dom']` added to `vite.config.ts`
- [x] `@tanstack/react-query-devtools` removed (pnpm context isolation bug)
- [x] TypeScript: zero errors (`pnpm tsc --noEmit`)

## Step 3: Verification 🟡
- [ ] User smoke test: `/`, `/products/:id`, `/orders`, `/orders/:id`, `/admin/products`
- [ ] Verify no duplicate network requests

## Step 4: Documentation & Completion
- [ ] Update `docs/memory/project-context.md`
- [ ] Archive plan and tracker

