import type { ResultModel } from '../types/api';
import type { Cart, AddToCartRequest, ApplyCouponRequest, CheckoutRequest } from '../types/cart';
import type { AxiosInstance } from 'axios';

export const cartApi = (apiClient: AxiosInstance) => ({
    async fetchCart(userId: string): Promise<ResultModel<Cart>> {
        const { data } = await apiClient.get<ResultModel<Cart>>(`/api/carts/${userId}`);
        return data;
    },

    async addToCart(request: AddToCartRequest): Promise<ResultModel<boolean>> {
        const { data } = await apiClient.post<ResultModel<boolean>>('/api/carts', request);
        return data;
    },

    async removeFromCart(cartDetailsId: string): Promise<ResultModel<boolean>> {
        const { data } = await apiClient.delete<ResultModel<boolean>>(`/api/carts/item/${cartDetailsId}`);
        return data;
    },

    async applyCoupon(request: ApplyCouponRequest): Promise<ResultModel<boolean>> {
        const { data } = await apiClient.post<ResultModel<boolean>>('/api/carts/coupon', request);
        return data;
    },

    async removeCoupon(): Promise<ResultModel<boolean>> {
        const { data } = await apiClient.delete<ResultModel<boolean>>('/api/carts/coupon');
        return data;
    },

    async checkout(request: CheckoutRequest): Promise<ResultModel<boolean>> {
        const { data } = await apiClient.post<ResultModel<boolean>>('/api/carts/checkout', request);
        return data;
    }
});
