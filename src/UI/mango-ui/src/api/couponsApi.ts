import type { ResultModel } from '../types/api';
import type { AxiosInstance } from 'axios';

export interface Coupon {
    id: string;
    code: string;
    discountAmount: number;
}

export const couponsApi = (apiClient: AxiosInstance) => ({
    async fetchCoupon(couponCode: string): Promise<ResultModel<Coupon>> {
        const { data } = await apiClient.get<ResultModel<Coupon>>(`/api/coupons/${couponCode}`);
        return data;
    }
});
