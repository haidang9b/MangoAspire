import type { AxiosInstance } from 'axios';
import type { ResultModel, PaginatedItems, OrderDto, OrderDetailDto } from '../types';

export const ordersApi = (apiClient: AxiosInstance) => ({
    async fetchOrders(pageIndex = 1, pageSize = 10): Promise<ResultModel<PaginatedItems<OrderDto>>> {
        const { data } = await apiClient.get<ResultModel<PaginatedItems<OrderDto>>>('/api/orders', {
            params: { pageIndex, pageSize }
        });
        return data;
    },

    async fetchOrderById(id: string): Promise<ResultModel<OrderDetailDto>> {
        const { data } = await apiClient.get<ResultModel<OrderDetailDto>>(`/api/orders/${id}`);
        return data;
    }
});
