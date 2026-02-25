import { useNavigate } from 'react-router-dom';
import { ROUTES } from '../../constants';
import './ConfirmationPage.css';

export function ConfirmationPage() {
    const navigate = useNavigate();

    return (
        <div className="confirmation-page">
            <div className="confirmation-card">
                <div className="confirmation-icon">✓</div>
                <h1>Order Confirmed!</h1>
                <p>Thank you for your order. We've received it and will start preparing it soon.</p>
                <div className="confirmation-actions">
                    <button className="btn-primary" onClick={() => navigate(ROUTES.HOME)}>Continue Shopping</button>
                    <button className="btn-secondary" onClick={() => navigate(ROUTES.ORDERS)}>View My Orders</button>
                </div>
            </div>
        </div>
    );
}
