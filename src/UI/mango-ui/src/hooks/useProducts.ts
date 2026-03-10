import { useQuery } from '@tanstack/react-query';
import { useApi } from './useApi';
import { QUERY_KEYS } from '@/constants';
import type { Product } from '@/types/product';

interface UseProductsOptions {
    pageIndex: number;
    pageSize?: number;
    catalogTypeId?: number;
    search?: string;
}

interface UseProductsResult {
    products: Product[];
    totalCount: number;
    isLoading: boolean;
    isError: boolean;
    error: string | null;
    refetch: () => Promise<unknown>;
}

export function useProducts({
    pageIndex,
    pageSize = 12,
    catalogTypeId,
    search,
}: UseProductsOptions): UseProductsResult {
    const { products: productsService } = useApi();

    const { data, isPending, error, refetch } = useQuery({
        queryKey: QUERY_KEYS.products({ pageIndex, pageSize, catalogTypeId, search }),
        queryFn: async () => {
            const result = await productsService.fetchProducts(pageIndex, pageSize, catalogTypeId, search);
            if (result.isError || !result.data) throw new Error(result.errorMessage ?? 'Failed to load products.');
            return result.data;
        },
    });

    return {
        products: data?.data ?? [],
        totalCount: data?.count ?? 0,
        isLoading: isPending,
        isError: !!error,
        error: error ? error.message : null,
        refetch,
    };
}
