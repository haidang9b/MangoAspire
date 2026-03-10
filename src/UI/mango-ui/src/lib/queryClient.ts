import { QueryClient } from '@tanstack/react-query';

/**
 * Singleton QueryClient for the entire application.
 *
 * Default settings:
 * - staleTime: 60s  — matches the old useFetch TTL; no refetch within 1 min
 * - retry: 1        — one automatic retry on failure
 * - refetchOnWindowFocus: true — re-check stale data when tab is re-focused
 */
export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 60_000,
            retry: 1,
            refetchOnWindowFocus: true,
        },
    },
});
