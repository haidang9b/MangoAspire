import { useFetch } from './useFetch';
import { useApi } from './useApi';
import type { PaginatedItems } from '../types/api';
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

    const cacheKey = `products-${pageIndex}-${pageSize}-${catalogTypeId ?? 'all'}`;

    const { data, isLoading, error, reload } = useFetch<PaginatedItems<Product>>(
        cacheKey,
        async () => {
            const result = await productsService.fetchProducts(pageIndex, pageSize, catalogTypeId);
            if (result.isError || !result.data) throw new Error(result.errorMessage ?? 'Failed to load products.');
            return result.data;
        }
    );

    return {
        products: data?.data ?? [],
        totalCount: data?.count ?? 0,
        isLoading,
        error,
        reload,
    };
}
