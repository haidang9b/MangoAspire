import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useAuth } from '../../auth/AuthContext';
import { useCart } from '../../context/CartContext';
import { useApi } from '../../hooks/useApi';
import { PageMetadata } from '../../components/PageMetadata';
import type { CheckoutRequest } from '../../types/cart';
import './CheckoutPage.css';

interface CheckoutFormData {
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    pickupDate: string;
    cardNumber: string;
    cvv: string;
    expiryMonthYear: string;
}

export function CheckoutPage() {
    const { user } = useAuth();
    const { cart } = useCart();
    const { cart: cartService } = useApi();
    const navigate = useNavigate();

    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState<string | null>(null);

    const {
        register,
        handleSubmit,
        formState: { errors }
    } = useForm<CheckoutFormData>({
        defaultValues: {
            firstName: user?.profile.given_name || user?.profile.name?.split(' ')[0] || '',
            lastName: user?.profile.family_name || user?.profile.name?.split(' ').slice(1).join(' ') || '',
            email: user?.profile.email || '',
            phone: '',
            pickupDate: new Date(Date.now() + 3600000).toISOString().slice(0, 16),
            cardNumber: '',
            cvv: '',
            expiryMonthYear: '',
        }
    });

    if (!cart || cart.cartDetails.length === 0) {
        return (
            <div className="checkout-page checkout-page--empty">
                <div className="empty-msg">
                    <h2>Your cart is empty</h2>
                    <p>Add some delicious items before checking out!</p>
                    <button className="btn-primary" onClick={() => navigate('/')}>Back to Shop</button>
                </div>
            </div>
        );
    }

    const onSubmit = async (data: CheckoutFormData) => {
        setSubmitError(null);
        setIsSubmitting(true);

        try {
            const request: CheckoutRequest = {
                ...data,
                couponCode: cart.cartHeader.couponCode || '',
                discountTotal: cart.cartHeader.discountTotal,
                orderTotal: cart.cartHeader.orderTotal,
                cartTotalItems: cart.cartDetails.length,
            };

            const result = await cartService.checkout(request);

            if (!result.isError) {
                navigate('/confirmation');
            } else {
                setSubmitError(result.errorMessage || 'Failed to place order.');
            }
        } catch (err) {
            setSubmitError('An error occurred while placing your order.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="checkout-page">
            <PageMetadata
                title="Checkout | Mango Store"
                description="Complete your order and enjoy our premium mangoes soon!"
            />
            <h1 className="checkout-page__title">Checkout</h1>

            {submitError && <div className="checkout-error-alert">{submitError}</div>}

            <form className="checkout-container" onSubmit={handleSubmit(onSubmit)}>
                <section className="checkout-form">
                    <div className="checkout-section">
                        <h3>Personal Details</h3>
                        <div className="form-grid">
                            <div className="form-field">
                                <label htmlFor="firstName">First Name</label>
                                <input id="firstName" type="text" {...register('firstName', { required: 'Required' })} />
                                {errors.firstName && <span className="error-text">{errors.firstName.message}</span>}
                            </div>
                            <div className="form-field">
                                <label htmlFor="lastName">Last Name</label>
                                <input id="lastName" type="text" {...register('lastName', { required: 'Required' })} />
                                {errors.lastName && <span className="error-text">{errors.lastName.message}</span>}
                            </div>
                            <div className="form-field">
                                <label htmlFor="email">Email</label>
                                <input
                                    id="email"
                                    type="email"
                                    {...register('email', {
                                        required: 'Required',
                                        pattern: {
                                            value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                                            message: 'Invalid email address'
                                        }
                                    })}
                                />
                                {errors.email && <span className="error-text">{errors.email.message}</span>}
                            </div>
                            <div className="form-field">
                                <label htmlFor="phone">Phone</label>
                                <input
                                    id="phone"
                                    type="tel"
                                    {...register('phone', {
                                        required: 'Required',
                                        pattern: {
                                            value: /^\d{10,15}$/,
                                            message: '10-15 digits required'
                                        }
                                    })}
                                />
                                {errors.phone && <span className="error-text">{errors.phone.message}</span>}
                            </div>
                        </div>
                    </div>

                    <div className="checkout-section">
                        <h3>Delivery Information</h3>
                        <div className="form-field">
                            <label htmlFor="pickupDate">Pickup Time (Min 1hr from now)</label>
                            <input
                                id="pickupDate"
                                type="datetime-local"
                                {...register('pickupDate', {
                                    required: 'Required',
                                    validate: (value) => {
                                        const pickup = new Date(value);
                                        const minTime = new Date(Date.now() + 3500000); // ~1 hour buffer
                                        return pickup > minTime || 'Pickup must be at least 1 hour from now';
                                    }
                                })}
                            />
                            {errors.pickupDate && <span className="error-text">{errors.pickupDate.message}</span>}
                        </div>
                    </div>

                    <div className="checkout-section">
                        <h3>Payment Details</h3>
                        <div className="form-grid">
                            <div className="form-field form-field--wide">
                                <label htmlFor="cardNumber">Card Number</label>
                                <input
                                    id="cardNumber"
                                    type="text"
                                    placeholder="0000 0000 0000 0000"
                                    {...register('cardNumber', {
                                        required: 'Required',
                                        pattern: {
                                            value: /^\d{16}$/,
                                            message: '16 digits required'
                                        }
                                    })}
                                />
                                {errors.cardNumber && <span className="error-text">{errors.cardNumber.message}</span>}
                            </div>
                            <div className="form-field">
                                <label htmlFor="cvv">CVV</label>
                                <input
                                    id="cvv"
                                    type="text"
                                    placeholder="123"
                                    maxLength={4}
                                    {...register('cvv', {
                                        required: 'Required',
                                        pattern: {
                                            value: /^\d{3,4}$/,
                                            message: '3-4 digits required'
                                        }
                                    })}
                                />
                                {errors.cvv && <span className="error-text">{errors.cvv.message}</span>}
                            </div>
                            <div className="form-field">
                                <label htmlFor="expiryMonthYear">Expiry (MMYY)</label>
                                <input
                                    id="expiryMonthYear"
                                    type="text"
                                    placeholder="MMYY"
                                    maxLength={4}
                                    {...register('expiryMonthYear', {
                                        required: 'Required',
                                        pattern: {
                                            value: /^(0[1-9]|1[0-2])\d{2}$/,
                                            message: 'Use MMYY format'
                                        }
                                    })}
                                />
                                {errors.expiryMonthYear && <span className="error-text">{errors.expiryMonthYear.message}</span>}
                            </div>
                        </div>
                    </div>
                </section>

                <aside className="checkout-summary">
                    <div className="summary-card">
                        <h3>Order Review</h3>
                        <div className="summary-items">
                            {cart.cartDetails.map((item) => (
                                <div key={item.id} className="summary-item">
                                    <span className="summary-item__name">{item.product?.name}</span>
                                    <span className="summary-item__count">x{item.count}</span>
                                    <span className="summary-item__price">
                                        ${((item.product?.price || 0) * item.count).toFixed(2)}
                                    </span>
                                </div>
                            ))}
                        </div>

                        <div className="summary-totals">
                            <div className="summary-row summary-row--total">
                                <span className="text-danger">Order Total</span>
                                <span className="text-danger">${cart.cartHeader.orderTotal.toFixed(2)}</span>
                            </div>
                            {cart.cartHeader.discountTotal > 0 && (
                                <div className="summary-row summary-row--discount">
                                    <span className="text-success">Order Discount</span>
                                    <span className="text-success">-${cart.cartHeader.discountTotal.toFixed(2)}</span>
                                </div>
                            )}
                        </div>

                        <button
                            type="submit"
                            className="btn-primary checkout-submit-btn"
                            disabled={isSubmitting}
                        >
                            {isSubmitting ? 'Placing Order...' : 'Place Order'}
                        </button>
                    </div>
                </aside>
            </form>
        </div>
    );
}


