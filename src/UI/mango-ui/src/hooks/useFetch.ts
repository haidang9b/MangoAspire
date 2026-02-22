import { useCallback, useEffect, useRef, useState } from 'react';

interface CacheEntry<T> {
    data: T;
    timestamp: number;
}

// Module-level cache shared across all hook instances
const cache = new Map<string, CacheEntry<unknown>>();

interface UseFetchOptions {
    /** Cache time-to-live in milliseconds. Default: 60000 (1 min) */
    ttl?: number;
    /** Set to false to skip the fetch (e.g. when a required param is missing) */
    enabled?: boolean;
}

interface UseFetchResult<T> {
    data: T | null;
    isLoading: boolean;
    error: string | null;
    reload: () => void;
}

/**
 * Generic fetch hook with stale-while-revalidate caching.
 *
 * On first mount: shows loading until data arrives, then caches it.
 * On subsequent mounts (within TTL): returns cached data immediately
 * (no loading flash), then silently re-fetches in the background.
 *
 * @param cacheKey  Unique string key for this request (include params)
 * @param fetcher   Async function that returns the data (or throws)
 * @param options   ttl, enabled
 */
export function useFetch<T>(
    cacheKey: string,
    fetcher: () => Promise<T>,
    options: UseFetchOptions = {}
): UseFetchResult<T> {
    const { ttl = 60_000, enabled = true } = options;

    const getCached = (): T | null => {
        const entry = cache.get(cacheKey) as CacheEntry<T> | undefined;
        if (!entry) return null;
        if (Date.now() - entry.timestamp > ttl) {
            cache.delete(cacheKey);
            return null;
        }
        return entry.data;
    };

    const cached = getCached();

    const [data, setData] = useState<T | null>(cached);
    const [isLoading, setIsLoading] = useState(!cached && enabled);
    const [error, setError] = useState<string | null>(null);
    const [tick, setTick] = useState(0);

    // Track the latest cacheKey to handle key changes mid-flight
    const keyRef = useRef(cacheKey);
    keyRef.current = cacheKey;

    const reload = useCallback(() => {
        cache.delete(cacheKey);
        setTick((t) => t + 1);
    }, [cacheKey]);

    useEffect(() => {
        if (!enabled) return;

        let cancelled = false;
        const currentKey = cacheKey;

        const fresh = getCached();
        if (fresh) {
            setData(fresh);
            setIsLoading(false);
            // Still re-fetch in background to keep data fresh
        } else {
            setIsLoading(true);
        }

        const run = async () => {
            try {
                const result = await fetcher();
                if (cancelled || keyRef.current !== currentKey) return;
                cache.set(currentKey, { data: result, timestamp: Date.now() });
                setData(result);
                setError(null);
            } catch (err) {
                if (cancelled) return;
                setError(err instanceof Error ? err.message : 'An unexpected error occurred');
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        };

        run();
        return () => { cancelled = true; };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [cacheKey, enabled, tick]);

    return { data, isLoading, error, reload };
}
