import { useMemo } from 'react';
import { useApiClient } from './useApiClient';
import { productsApi } from '../api/productsApi';
import { cartApi } from '../api/cartApi';
import { couponsApi } from '../api/couponsApi';
import { createChatApi } from '../api/chat';
import { ordersApi } from '../api/ordersApi';

export function useApi() {
    const apiClient = useApiClient();

    const api = useMemo(() => ({
        products: productsApi(apiClient),
        cart: cartApi(apiClient),
        coupons: couponsApi(apiClient),
        chat: createChatApi(apiClient),
        orders: ordersApi(apiClient),
    }), [apiClient]);

    return api;
}
