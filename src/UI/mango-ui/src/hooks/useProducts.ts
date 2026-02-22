import { useCallback, useEffect, useState } from 'react';
import { useApi } from './useApi';
import type { Product } from '../types/product';

interface UseProductsOptions {
    pageIndex: number;
    pageSize?: number;
    catalogTypeId?: number;
}

interface UseProductsResult {
    products: Product[];
    totalCount: number;
    isLoading: boolean;
    error: string | null;
    reload: () => void;
}

export function useProducts({
    pageIndex,
    pageSize = 12,
    catalogTypeId,
}: UseProductsOptions): UseProductsResult {
    const { products: productsService } = useApi();
    const [products, setProducts] = useState<Product[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [tick, setTick] = useState(0);

    const reload = useCallback(() => setTick((t) => t + 1), []);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            setIsLoading(true);
            setError(null);
            try {
                const result = await productsService.fetchProducts(pageIndex, pageSize, catalogTypeId);
                if (cancelled) return;
                if (!result.isError && result.data) {
                    setProducts(result.data.data);
                    setTotalCount(result.data.count);
                } else {
                    setError(result.errorMessage ?? 'Failed to load products.');
                }
            } catch {
                if (!cancelled) setError('Could not connect to the products service.');
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        };

        load();
        return () => { cancelled = true; };
    }, [pageIndex, pageSize, catalogTypeId, tick]);

    return { products, totalCount, isLoading, error, reload };
}
