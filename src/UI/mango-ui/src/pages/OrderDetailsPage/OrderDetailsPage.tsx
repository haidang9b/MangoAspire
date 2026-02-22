import { useEffect, useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useApi } from '../../hooks/useApi';
import type { OrderDetailDto } from '../../types/order';
import './OrderDetailsPage.css';

export function OrderDetailsPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { orders: ordersService } = useApi();
    const [order, setOrder] = useState<OrderDetailDto | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (!id) return;

        const loadOrderDetails = async () => {
            try {
                const result = await ordersService.fetchOrderById(id);
                if (!result.isError && result.data) {
                    setOrder(result.data);
                } else {
                    setError(result.errorMessage || 'Failed to load order details');
                }
            } catch (err) {
                setError('An unexpected error occurred');
            } finally {
                setLoading(false);
            }
        };

        loadOrderDetails();
    }, [id, ordersService]);

    if (loading) return <div className="page-loading">Fetching details...</div>;
    if (error || !order) return (
        <div className="container py-5 text-center">
            <div className="error-banner mb-4">{error || 'Order not found'}</div>
            <button onClick={() => navigate('/orders')} className="btn btn-primary">Back to Orders</button>
        </div>
    );

    return (
        <div className="order-details-page container py-5">
            <div className="page-nav mb-4">
                <Link to="/orders" className="back-link">← Back to My Orders</Link>
            </div>

            <div className="details-header mb-5">
                <div className="title-area">
                    <h1>Order Details</h1>
                    <span className="order-id">#{order.id.toUpperCase()}</span>
                </div>
                <div className="status-badge" data-status={order.status.toLowerCase()}>
                    {order.status}
                </div>
            </div>

            <div className="details-grid">
                <section className="details-card info-section">
                    <h2 className="section-title">Shipping Information</h2>
                    <div className="info-grid">
                        <div className="info-item">
                            <span className="label">Full Name</span>
                            <span className="value">{order.firstName} {order.lastName}</span>
                        </div>
                        <div className="info-item">
                            <span className="label">Email Address</span>
                            <span className="value">{order.email}</span>
                        </div>
                        <div className="info-item">
                            <span className="label">Phone Number</span>
                            <span className="value">{order.phone || 'N/A'}</span>
                        </div>
                        <div className="info-item">
                            <span className="label">Order Date</span>
                            <span className="value">{new Date(order.orderTime).toLocaleString()}</span>
                        </div>
                    </div>
                </section>

                <section className="details-card items-section">
                    <h2 className="section-title">Order Items</h2>
                    <div className="items-list">
                        {order.items.map((item) => (
                            <div key={item.id} className="order-item">
                                <div className="item-info">
                                    <span className="item-name">{item.productName}</span>
                                    <span className="item-qty">Qty: {item.count}</span>
                                </div>
                                <span className="item-price">${(item.price * item.count).toFixed(2)}</span>
                            </div>
                        ))}
                    </div>

                    <div className="summary-area mt-4">
                        <div className="summary-row">
                            <span>Subtotal</span>
                            <span>${(order.orderTotal + (order.discountTotal || 0)).toFixed(2)}</span>
                        </div>
                        {order.discountTotal > 0 && (
                            <div className="summary-row discount">
                                <span>Discount {order.couponCode && `(${order.couponCode})`}</span>
                                <span>-${order.discountTotal.toFixed(2)}</span>
                            </div>
                        )}
                        <div className="summary-row total">
                            <span>Total Amount</span>
                            <span>${order.orderTotal.toFixed(2)}</span>
                        </div>
                    </div>

                    {order.cancelReason && (
                        <div className="cancel-notice mt-4">
                            <strong>Cancellation Reason:</strong> {order.cancelReason}
                        </div>
                    )}
                </section>
            </div>
        </div>
    );
};

