export interface ProductFilters {
    pageIndex: number;
    pageSize: number;
    catalogTypeId?: number;
    search?: string;
}

export interface OrderFilters {
    pageIndex: number;
    pageSize: number;
}

/**
 * Centralized TanStack Query key factories.
 * Array-based keys enable partial invalidation — e.g.
 *   invalidateQueries({ queryKey: ['products'] })
 * invalidates ALL product queries regardless of page/filter params.
 */
export const QUERY_KEYS = {
    /** Base key — use for broad invalidation (e.g. after create/edit/delete) */
    productsAll: () => ['products'] as const,
    products: (filters: ProductFilters) => [...QUERY_KEYS.productsAll(), filters] as const,
    productDetail: (id: string | undefined) => [...QUERY_KEYS.productsAll(), 'detail', id] as const,
    catalogTypes: () => ['catalog-types'] as const,
    orders: (filters: OrderFilters) => ['orders-list', filters] as const,
    orderDetail: (id: string | undefined) => ['order-detail', id] as const,
} as const;

/** @deprecated Use QUERY_KEYS instead */
export const CACHE_KEYS = {
    PRODUCTS: 'products',
    CATALOG_TYPES: 'catalog-types',
    ORDERS: 'orders-list',
    ORDER_DETAILS: 'order-detail',
} as const;
