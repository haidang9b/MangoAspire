import type { AxiosInstance } from 'axios';
import type { ResultModel, PaginatedItems, Product, CatalogType } from '../types';

export interface CreateProductRequest {
    name: string;
    price: number;
    description: string;
    categoryName: string;
    catalogTypeId?: number;
    imageUrl: string;
    stock: number;
}

export interface UpdateProductRequest extends CreateProductRequest {
    id: string;
}

export const productsApi = (apiClient: AxiosInstance) => ({
    async fetchProducts(
        pageIndex = 1,
        pageSize = 12,
        catalogTypeId?: number,
        search?: string
    ): Promise<ResultModel<PaginatedItems<Product>>> {
        const params: Record<string, string | number> = { pageIndex, pageSize };
        if (catalogTypeId != null) params.catalogTypeId = catalogTypeId;
        if (search) params.search = search;

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
    },

    async createProduct(payload: CreateProductRequest): Promise<ResultModel<string>> {
        const { data } = await apiClient.post<ResultModel<string>>('/api/products', payload);
        return data;
    },

    async updateProduct(payload: UpdateProductRequest): Promise<ResultModel<boolean>> {
        const { data } = await apiClient.put<ResultModel<boolean>>('/api/products', payload);
        return data;
    },

    async deleteProduct(id: string): Promise<ResultModel<boolean>> {
        const { data } = await apiClient.delete<ResultModel<boolean>>(`/api/products/${id}`);
        return data;
    },
});
