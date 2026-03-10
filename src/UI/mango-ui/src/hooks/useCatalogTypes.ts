import { useQuery } from '@tanstack/react-query';
import { useApi } from './useApi';
import { QUERY_KEYS } from '@/constants';
import type { CatalogType } from '@/types/product';

interface UseCatalogTypesResult {
    catalogTypes: CatalogType[];
    isError: boolean;
    error: string | null;
}

/**
 * Fetches and caches the list of catalog types.
 * Shared between ProductsPage and AdminProductsPage.
 * TanStack Query deduplicates the network request automatically when both mount.
 */
export function useCatalogTypes(): UseCatalogTypesResult {
    const { products: productsService } = useApi();

    const { data: catalogTypes = [], isError, error } = useQuery<CatalogType[]>({
        queryKey: QUERY_KEYS.catalogTypes(),
        queryFn: async () => {
            const result = await productsService.fetchCatalogTypes();
            if (result.isError || !result.data) throw new Error('Failed to load categories.');
            return result.data;
        },
    });

    return {
        catalogTypes,
        isError,
        error: error ? error.message : null,
    };
}
