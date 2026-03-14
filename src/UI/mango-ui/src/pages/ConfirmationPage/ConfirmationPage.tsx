import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ROUTES } from '@/constants';
import './ConfirmationPage.css';

export function ConfirmationPage() {
    const navigate = useNavigate();
    const { t } = useTranslation();

    return (
        <div className="confirmation-page">
            <div className="confirmation-card">
                <div className="confirmation-icon">✓</div>
                <h1>{t('orders.confirmed')}</h1>
                <p>{t('orders.thankYou')}</p>
                <div className="confirmation-actions">
                    <button className="btn-primary" onClick={() => navigate(ROUTES.HOME)}>{t('common.back')} {t('products.title')}</button>
                    <button className="btn-secondary" onClick={() => navigate(ROUTES.ORDERS)}>View {t('nav.orders')}</button>
                </div>
            </div>
        </div>
    );
}
