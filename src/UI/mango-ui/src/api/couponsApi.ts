import type { AxiosInstance } from 'axios';
import type { ResultModel, Coupon } from '../types';

export const couponsApi = (apiClient: AxiosInstance) => ({
    async fetchCoupon(couponCode: string): Promise<ResultModel<Coupon>> {
        const { data } = await apiClient.get<ResultModel<Coupon>>(`/api/coupons/${couponCode}`);
        return data;
    }
});
