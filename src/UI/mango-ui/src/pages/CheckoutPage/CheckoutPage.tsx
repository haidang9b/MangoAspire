import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useAuth, useCart, useApi } from '@/hooks';
import { useTranslation } from 'react-i18next';
import { PageMetadata, TextBox } from '@/components';
import { ROUTES } from '@/constants';
import type { CheckoutRequest, CartDetails } from '@/types';
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
    const { t } = useTranslation();

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
                    <h2>{t('cart.empty')}</h2>
                    <p>Add some delicious items before checking out!</p>
                    <button className="btn-primary" onClick={() => navigate(ROUTES.HOME)}>{t('common.back')} {t('products.title')}</button>
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
                navigate(ROUTES.CONFIRMATION);
            } else {
                setSubmitError(result.errorMessage || 'Failed to place order.');
            }
        } catch {
            setSubmitError('An error occurred while placing your order.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="checkout-page">
            <PageMetadata
                title={`${t('checkout.title')} | Mango Store`}
                description="Complete your order and enjoy our premium mangoes soon!"
            />
            <h1 className="checkout-page__title">{t('checkout.title')}</h1>

            {submitError && <div className="checkout-error-alert">{submitError}</div>}

            <form className="checkout-container" onSubmit={handleSubmit(onSubmit)}>
                <section className="checkout-form">
                    <div className="checkout-section">
                        <h3>{t('checkout.personalDetails', 'Personal Details')}</h3>
                        <div className="form-grid">
                            <TextBox
                                label={t('checkout.firstName', 'First Name')}
                                error={errors.firstName?.message}
                                {...register('firstName', { required: 'Required' })}
                            />
                            <TextBox
                                label={t('checkout.lastName', 'Last Name')}
                                error={errors.lastName?.message}
                                {...register('lastName', { required: 'Required' })}
                            />
                            <TextBox
                                label={t('checkout.email', 'Email Address')}
                                type="email"
                                error={errors.email?.message}
                                {...register('email', {
                                    required: 'Required',
                                    pattern: {
                                        value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                                        message: 'Invalid email address'
                                    }
                                })}
                            />
                            <TextBox
                                label={t('checkout.phone', 'Phone Number')}
                                type="tel"
                                error={errors.phone?.message}
                                {...register('phone', {
                                    required: 'Required',
                                    pattern: {
                                        value: /^\d{10,15}$/,
                                        message: '10-15 digits required'
                                    }
                                })}
                            />
                        </div>
                    </div>

                    <div className="checkout-section">
                        <h3>{t('checkout.deliveryInfo', 'Delivery Information')}</h3>
                        <TextBox
                            label={t('checkout.pickupTime', 'Pickup Time (Min 1hr from now)')}
                            type="datetime-local"
                            error={errors.pickupDate?.message}
                            {...register('pickupDate', {
                                required: 'Required',
                                validate: (value) => {
                                    const pickup = new Date(value);
                                    const minTime = new Date(Date.now() + 3500000); // ~1 hour buffer
                                    return pickup > minTime || 'Pickup must be at least 1 hour from now';
                                }
                            })}
                        />
                    </div>

                    <div className="checkout-section">
                        <h3>{t('checkout.paymentDetails', 'Payment Details')}</h3>
                        <div className="form-grid">
                            <TextBox
                                label={t('checkout.cardNumber', 'Card Number')}
                                placeholder="0000 0000 0000 0000"
                                containerClassName="form-field--wide"
                                error={errors.cardNumber?.message}
                                {...register('cardNumber', {
                                    required: 'Required',
                                    pattern: {
                                        value: /^\d{16}$/,
                                        message: '16 digits required'
                                    }
                                })}
                            />
                            <TextBox
                                label={t('checkout.cvv', 'CVV')}
                                placeholder="123"
                                maxLength={4}
                                error={errors.cvv?.message}
                                {...register('cvv', {
                                    required: 'Required',
                                    pattern: {
                                        value: /^\d{3,4}$/,
                                        message: '3-4 digits required'
                                    }
                                })}
                            />
                            <TextBox
                                label={t('checkout.expiry', 'Expiry (MMYY)')}
                                placeholder="MMYY"
                                maxLength={4}
                                error={errors.expiryMonthYear?.message}
                                {...register('expiryMonthYear', {
                                    required: 'Required',
                                    pattern: {
                                        value: /^(0[1-9]|1[0-2])\d{2}$/,
                                        message: 'Use MMYY format'
                                    }
                                })}
                            />
                        </div>
                    </div>
                </section>

                <aside className="checkout-summary">
                    <div className="summary-card">
                        <h3>{t('checkout.orderSummary')}</h3>
                        <div className="summary-items">
                            {cart.cartDetails.map((item: CartDetails) => (
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
                                <span className="text-danger">{t('cart.total')}</span>
                                <span className="text-danger">${cart.cartHeader.orderTotal.toFixed(2)}</span>
                            </div>
                            {cart.cartHeader.discountTotal > 0 && (
                                <div className="summary-row summary-row--discount">
                                    <span className="text-success">{t('cart.discount')}</span>
                                    <span className="text-success">-${cart.cartHeader.discountTotal.toFixed(2)}</span>
                                </div>
                            )}
                        </div>

                        <button
                            type="submit"
                            className="btn-primary checkout-submit-btn"
                            disabled={isSubmitting}
                        >
                            {isSubmitting ? t('common.loading') : t('checkout.placeOrder')}
                        </button>
                    </div>
                </aside>
            </form>
        </div>
    );
}
