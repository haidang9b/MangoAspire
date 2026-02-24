import { createContext } from 'react';
import type { Cart } from '../types/cart';

export interface CartContextValue {
    cart: Cart | null;
    isLoading: boolean;
    error: string | null;
    addToCart: (productId: string, count: number) => Promise<boolean>;
    removeItem: (cartDetailsId: string) => Promise<boolean>;
    applyCoupon: (couponCode: string) => Promise<boolean>;
    removeCoupon: () => Promise<boolean>;
    refresh: () => void;
    cartCount: number;
}

export const CartContext = createContext<CartContextValue | null>(null);
