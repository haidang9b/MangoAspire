import { useNavigate, useParams, Link } from 'react-router-dom';
import { useApi, useFetch } from '@/hooks';
import { useTranslation } from 'react-i18next';
import { CACHE_KEYS, ROUTES } from '@/constants';
import type { OrderDetailDto } from '@/types';
import './OrderDetailsPage.css';

export function OrderDetailsPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { orders: ordersService } = useApi();
    const { t } = useTranslation();

    const { data: order, isLoading, error } = useFetch<OrderDetailDto>(
        `${CACHE_KEYS.ORDER_DETAILS}-${id}`,
        async () => {
            const result = await ordersService.fetchOrderById(id!);
            if (result.isError) throw new Error(result.errorMessage || 'Failed to load order details');
            return result.data;
        },
        { enabled: !!id }
    );

    if (isLoading) return <div className="page-loading">{t('common.loading')}</div>;
    if (error || !order) return (
        <div className="container py-5 text-center">
            <div className="error-banner mb-4">{error || t('orders.notFound')}</div>
            <button onClick={() => navigate(ROUTES.ORDERS)} className="btn btn-primary">{t('common.back')} {t('nav.orders')}</button>
        </div>
    );

    return (
        <div className="order-details-page container py-5">
            <div className="page-nav mb-4">
                <Link to={ROUTES.ORDERS} className="back-link">← {t('common.back')} {t('nav.orders')}</Link>
            </div>

            <div className="details-header mb-5">
                <div className="title-area">
                    <h1>{t('orders.title')}</h1>
                    <span className="order-id">#{order.id.toUpperCase()}</span>
                </div>
                <div className="status-badge" data-status={order.status.toLowerCase()}>
                    {order.status}
                </div>
            </div>

            <div className="details-grid">
                <section className="details-card info-section">
                    <h2 className="section-title">{t('orders.shippingInfo')}</h2>
                    <div className="info-grid">
                        <div className="info-item">
                            <span className="label">{t('orders.fullName')}</span>
                            <span className="value">{order.firstName} {order.lastName}</span>
                        </div>
                        <div className="info-item">
                            <span className="label">{t('orders.email')}</span>
                            <span className="value">{order.email}</span>
                        </div>
                        <div className="info-item">
                            <span className="label">{t('orders.phone')}</span>
                            <span className="value">{order.phone || 'N/A'}</span>
                        </div>
                        <div className="info-item">
                            <span className="label">{t('orders.date')}</span>
                            <span className="value">{new Date(order.orderTime).toLocaleString()}</span>
                        </div>
                    </div>
                </section>

                <section className="details-card items-section">
                    <h2 className="section-title">{t('orders.items')}</h2>
                    <div className="items-list">
                        {order.items.map((item) => (
                            <div key={item.id} className="order-item">
                                <div className="item-info">
                                    <span className="item-name">{item.productName}</span>
                                    <span className="item-qty">{t('cart.quantity')}: {item.count}</span>
                                </div>
                                <span className="item-price">${(item.price * item.count).toFixed(2)}</span>
                            </div>
                        ))}
                    </div>

                    <div className="summary-area mt-4">
                        <div className="summary-row">
                            <span>{t('cart.subtotal')}</span>
                            <span>${(order.orderTotal + (order.discountTotal || 0)).toFixed(2)}</span>
                        </div>
                        {order.discountTotal > 0 && (
                            <div className="summary-row discount">
                                <span>{t('cart.discount')} {order.couponCode && `(${order.couponCode})`}</span>
                                <span>-${order.discountTotal.toFixed(2)}</span>
                            </div>
                        )}
                        <div className="summary-row total">
                            <span>{t('cart.total')}</span>
                            <span>${order.orderTotal.toFixed(2)}</span>
                        </div>
                    </div>

                    {order.cancelReason && (
                        <div className="cancel-notice mt-4">
                            <strong>{t('orders.cancelReason')}:</strong> {order.cancelReason}
                        </div>
                    )}
                </section>
            </div>
        </div>
    );
}
