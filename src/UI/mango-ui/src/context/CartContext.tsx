import {
    useEffect,
    useState,
    useCallback,
    type ReactNode,
} from 'react';
import type { Cart } from '../types';
import { useApi, useAuth } from '../hooks';
import { CartContext } from './CartContextObject';

export function CartProvider({ children }: { children: ReactNode }) {
    const { user } = useAuth();
    const api = useApi();
    const { cart: cartService } = api;
    const [cart, setCart] = useState<Cart | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [tick, setTick] = useState(0);

    const refresh = useCallback(() => setTick((t) => t + 1), []);

    useEffect(() => {
        if (!user?.profile.sub) {
            setCart(null);
            return;
        }

        let cancelled = false;
        const load = async () => {
            setIsLoading(true);
            setError(null);
            try {
                const result = await cartService.fetchCart(user.profile.sub);
                if (cancelled) return;

                if (!result.isError && result.data) {
                    const cartData = result.data;
                    let discountTotal = 0;

                    if (cartData.cartHeader.couponCode) {
                        try {
                            const couponResult = await api.coupons.fetchCoupon(cartData.cartHeader.couponCode);
                            if (!couponResult.isError && couponResult.data) {
                                discountTotal = couponResult.data.discountAmount;
                            }
                        } catch {
                            // Non-fatal: coupon might be invalid or service down
                        }
                    }

                    let orderTotal = 0;
                    cartData.cartDetails.forEach((item) => {
                        if (item.product) {
                            orderTotal += item.product.price * item.count;
                        }
                    });

                    // Match MVC logic: orderTotal = sum - discount
                    cartData.cartHeader.discountTotal = discountTotal;
                    cartData.cartHeader.orderTotal = orderTotal - discountTotal;

                    setCart(cartData);
                } else {
                    setCart(null);
                }
            } catch {
                if (!cancelled) setError('Failed to load cart.');
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        };

        load();
        return () => {
            cancelled = true;
        };
    }, [user, tick, api, cartService]);

    const handleAddToCart = async (productId: string, count: number) => {
        try {
            const result = await cartService.addToCart({ productId, count, couponCode: '' });
            if (!result.isError && result.data) {
                refresh();
                return true;
            }
            return false;
        } catch {
            return false;
        }
    };

    const handleRemoveItem = async (cartDetailsId: string) => {
        try {
            const result = await cartService.removeFromCart(cartDetailsId);
            if (!result.isError && result.data) {
                refresh();
                return true;
            }
            return false;
        } catch {
            return false;
        }
    };

    const handleApplyCoupon = async (couponCode: string) => {
        try {
            const result = await cartService.applyCoupon({ couponCode });
            if (!result.isError && result.data) {
                refresh();
                return true;
            }
            return false;
        } catch {
            return false;
        }
    };

    const handleRemoveCoupon = async () => {
        try {
            const result = await cartService.removeCoupon();
            if (!result.isError && result.data) {
                refresh();
                return true;
            }
            return false;
        } catch {
            return false;
        }
    };

    const cartCount = cart?.cartDetails.reduce((acc, item) => acc + item.count, 0) ?? 0;

    return (
        <CartContext.Provider
            value={{
                cart,
                isLoading,
                error,
                addToCart: handleAddToCart,
                removeItem: handleRemoveItem,
                applyCoupon: handleApplyCoupon,
                removeCoupon: handleRemoveCoupon,
                refresh,
                cartCount,
            }}
        >
            {children}
        </CartContext.Provider>
    );
}
