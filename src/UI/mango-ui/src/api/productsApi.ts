import type { ResultModel } from '../types/api';
import type { PaginatedItems } from '../types/api';
import type { Product, CatalogType } from '../types/product';
import type { AxiosInstance } from 'axios';

export const productsApi = (apiClient: AxiosInstance) => ({
    async fetchProducts(
        pageIndex = 0,
        pageSize = 12,
        catalogTypeId?: number
    ): Promise<ResultModel<PaginatedItems<Product>>> {
        const params: Record<string, string | number> = { pageIndex, pageSize };
        if (catalogTypeId != null) params.catalogTypeId = catalogTypeId;

        const { data } = await apiClient.get<ResultModel<PaginatedItems<Product>>>(
            '/api/products',
            { params }
        );
        return data;
    },

    async fetchCatalogTypes(): Promise<ResultModel<CatalogType[]>> {
        const { data } = await apiClient.get<ResultModel<CatalogType[]>>('/api/catalog-types');
        return data;
    },

    async fetchProductById(id: string): Promise<ResultModel<Product>> {
        const { data } = await apiClient.get<ResultModel<Product>>(`/api/products/${id}`);
        return data;
    }
});
