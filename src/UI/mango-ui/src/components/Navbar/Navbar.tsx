import { useNavigate, Link } from 'react-router-dom';
import { useAuth, useCart, useTheme } from '@/hooks';
import { useTranslation } from 'react-i18next';
import { LanguageSelector } from './LanguageSelector';
import { ROUTES } from '@/constants';
import { Roles, hasRole } from '@/constants/roles';
import './Navbar.css';

export function Navbar() {
    const { user, userInfo, isLoading, login, logout } = useAuth();
    const { theme, toggleTheme } = useTheme();
    const { cartCount } = useCart();
    const navigate = useNavigate();
    const { t } = useTranslation();

    return (
        <nav className="navbar">
            <div className="navbar__brand" onClick={() => navigate('/')} style={{ cursor: 'pointer' }}>
                <span className="navbar__logo">🥭</span>
                <span className="navbar__name">Mango Store</span>
            </div>
            <div className="navbar__actions">
                <LanguageSelector />
                <button
                    className="navbar__theme-toggle"
                    onClick={toggleTheme}
                    title={`Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`}
                >
                    {theme === 'dark' ? '🌞' : '🌙'}
                </button>
                {!isLoading && (
                    user ? (
                        <div className="navbar__user">
                            <Link to={ROUTES.CART} className="navbar__cart" style={{ cursor: 'pointer', position: 'relative', marginRight: '20px', textDecoration: 'none' }}>
                                <span style={{ fontSize: '1.4rem' }}>🛒</span>
                                {cartCount > 0 && (
                                    <span className="navbar__cart-count">{cartCount}</span>
                                )}
                            </Link>
                            <Link to={ROUTES.ORDERS} className="navbar__link" style={{ marginRight: '15px' }}>
                                {t('nav.orders')}
                            </Link>
                            {hasRole(userInfo?.role, Roles.Admin) && (
                                <Link to={ROUTES.ADMIN_PRODUCTS} className="navbar__link" style={{ marginRight: '15px' }}>
                                    ⚙️ {t('nav.admin')}
                                </Link>
                            )}
                            <span className="navbar__user-name">
                                {userInfo?.given_name || userInfo?.name || user.profile.name || user.profile.email || 'User'}
                            </span>
                            <button className="navbar__btn navbar__btn--logout" onClick={logout}>
                                {t('nav.logout')}
                            </button>
                        </div>
                    ) : (
                        <button className="navbar__btn navbar__btn--login" onClick={login}>
                            {t('nav.login')}
                        </button>
                    )
                )}
            </div>
        </nav>
    );
}
