import { Link } from 'react-router-dom';
import { useCart } from '../../context/CartContext';
import { PageMetadata } from '../../components/PageMetadata';
import { ROUTES } from '../../constants';
import './CartPage.css';

export function CartPage() {
    const { cart, isLoading, error, removeItem, applyCoupon, removeCoupon, refresh } = useCart();

    if (isLoading && !cart) {
        return (
            <div className="cart-page cart-page--loading">
                <p>Loading your cart...</p>
            </div>
        );
    }

    if (error) {
        return (
            <div className="cart-page cart-page--error">
                <p>⚠️ {error}</p>
                <button onClick={refresh}>Retry</button>
            </div>
        );
    }

    if (!cart || cart.cartDetails.length === 0) {
        return (
            <div className="cart-page cart-page--empty">
                <div className="empty-cart-msg">
                    <span style={{ fontSize: '4rem' }}>🛒</span>
                    <h2>Your cart is empty</h2>
                    <p>Go add some delicious mangoes!</p>
                    <Link to={ROUTES.HOME} className="cart-page__btn" style={{ textDecoration: 'none', display: 'block', textAlign: 'center' }}>
                        Back to Shop
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="cart-page">
            <PageMetadata
                title="Your Cart | Mango Store"
                description="Review your items and proceed to checkout for your delicious mangoes."
            />
            <h1 className="cart-page__title">Shopping Cart</h1>

            <div className="cart-page__container">
                <div className="cart-page__items">
                    {cart.cartDetails.map((item) => (
                        <div key={item.id} className="cart-item">
                            <div className="cart-item__image">
                                <img src={item.product?.imageUrl || 'https://placehold.co/100x100/1e1e2e/a6adc8?text=Mango'} alt={item.product?.name} />
                            </div>
                            <div className="cart-item__details">
                                <h3 className="cart-item__name">{item.product?.name}</h3>
                                <p className="cart-item__price">${item.product?.price.toFixed(2)}</p>
                            </div>
                            <div className="cart-item__quantity">
                                <span>Qty: {item.count}</span>
                            </div>
                            <div className="cart-item__total">
                                <p>${((item.product?.price ?? 0) * item.count).toFixed(2)}</p>
                            </div>
                            <button className="cart-item__remove" onClick={() => removeItem(item.id)}>
                                🗑️
                            </button>
                        </div>
                    ))}
                </div>

                <div className="cart-page__summary">
                    <div className="summary-card">
                        <h3>Order Summary</h3>

                        <div className="summary-row summary-row--total">
                            <span className="text-danger" style={{ fontSize: '1.3rem', fontWeight: 'bold' }}>Order Total</span>
                            <span className="text-danger" style={{ fontSize: '1.3rem', fontWeight: 'bold' }}>
                                ${cart.cartHeader.orderTotal.toFixed(2)}
                            </span>
                        </div>

                        {cart.cartHeader.discountTotal > 0 && (
                            <div className="summary-row summary-row--discount">
                                <span className="text-success">Order Discount</span>
                                <span className="text-success">-${cart.cartHeader.discountTotal.toFixed(2)}</span>
                            </div>
                        )}

                        <div className="cart-page__coupon">
                            {cart.cartHeader.couponCode ? (
                                <button className="coupon-btn coupon-btn--remove" onClick={() => removeCoupon()}>
                                    Remove Coupon
                                </button>
                            ) : (
                                <div className="coupon-input-group">
                                    <input type="text" id="couponCode" placeholder="Enter Coupon" />
                                    <button
                                        className="coupon-btn"
                                        onClick={() => {
                                            const input = document.getElementById('couponCode') as HTMLInputElement;
                                            if (input.value) applyCoupon(input.value);
                                        }}
                                    >
                                        Apply
                                    </button>
                                </div>
                            )}
                        </div>

                        <Link to={ROUTES.CHECKOUT} className="cart-page__btn cart-page__btn--checkout" style={{ textDecoration: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                            Checkout
                        </Link>
                    </div>
                </div>
            </div>
        </div>
    );
}
