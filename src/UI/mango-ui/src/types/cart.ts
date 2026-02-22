import type { Product } from './product';

export interface CartHeader {
    id: string;
    userId: string;
    couponCode?: string;
    orderTotal: number;
    discountTotal: number;
    firstName?: string;
    lastName?: string;
    pickupDateTime: string;
    phone?: string;
    email?: string;
    cardNumber?: string;
    cvv?: string;
    expiryMonthYear?: string;
}

export interface CartDetails {
    id: string;
    product?: Product;
    count: number;
}

export interface Cart {
    cartHeader: CartHeader;
    cartDetails: CartDetails[];
}

export interface AddToCartRequest {
    productId: string;
    count: number;
    couponCode?: string;
}

export interface ApplyCouponRequest {
    couponCode: string;
}

export interface CheckoutRequest {
    couponCode?: string;
    discountTotal: number;
    orderTotal: number;
    firstName: string;
    lastName: string;
    pickupDate: string;
    phone?: string;
    email: string;
    cardNumber: string;
    cvv: string;
    expiryMonthYear: string;
    cartTotalItems: number;
}
