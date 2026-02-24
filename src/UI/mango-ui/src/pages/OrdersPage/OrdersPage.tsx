import { Link } from 'react-router-dom';
import { useApi, useFetch } from '../../hooks';
import { PageMetadata } from '../../components';
import type { OrderDto, PaginatedItems } from '../../types';
import './OrdersPage.css';

export function OrdersPage() {
    const { orders: ordersService } = useApi();

    const { data: orders, isLoading, error } = useFetch<PaginatedItems<OrderDto>>(
        'orders-list',
        async () => {
            const result = await ordersService.fetchOrders();
            if (result.isError) throw new Error(result.errorMessage || 'Failed to load orders');
            return result.data;
        }
    );

    const getStatusColor = (status: string) => {
        switch (status.toLowerCase()) {
            case 'pending': return 'var(--status-pending)';
            case 'completed': return 'var(--status-success)';
            case 'cancelled': return 'var(--status-error)';
            case 'shipped': return 'var(--status-info)';
            default: return 'var(--text-sub)';
        }
    };

    if (isLoading) return <div className="page-loading">Loading order history...</div>;

    return (
        <div className="orders-page container py-5">
            <PageMetadata
                title="My Orders | Mango Store"
                description="View and track your previous orders from Mango Store."
            />
            <header className="page-header mb-5">
                <h1 className="page-title">My Orders</h1>
                <p className="page-subtitle">Track and manage your order history</p>
            </header>

            {error && <div className="error-banner mb-4">{error}</div>}

            {!orders || orders.data.length === 0 ? (
                <div className="empty-state">
                    <div className="empty-state__icon">📦</div>
                    <h2>No orders found</h2>
                    <p>It looks like you haven't placed any orders yet.</p>
                    <Link to="/" className="btn btn-primary mt-3">Start Shopping</Link>
                </div>
            ) : (
                <div className="orders-grid">
                    {orders.data.map((order) => (
                        <div key={order.id} className="order-card">
                            <div className="order-card__header">
                                <span className="order-card__id">Order #{order.id.split('-')[0].toUpperCase()}</span>
                                <span
                                    className="order-card__status"
                                    style={{ '--status-color': getStatusColor(order.status) } as React.CSSProperties}
                                >
                                    {order.status}
                                </span>
                            </div>
                            <div className="order-card__body">
                                <div className="order-card__row">
                                    <span className="label">Date</span>
                                    <span className="value">{new Date(order.orderTime).toLocaleDateString()}</span>
                                </div>
                                <div className="order-card__row">
                                    <span className="label">Items</span>
                                    <span className="value">{order.itemCount} item(s)</span>
                                </div>
                                <div className="order-card__row">
                                    <span className="label">Total</span>
                                    <span className="value value--total">${order.orderTotal.toFixed(2)}</span>
                                </div>
                            </div>
                            <div className="order-card__footer">
                                <Link to={`/orders/${order.id}`} className="btn btn-outline btn-block">
                                    View Details
                                </Link>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
