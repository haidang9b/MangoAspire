import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { useCart } from '../../context/CartContext';
import { useTheme } from '../../context/ThemeContext';
import { Roles, hasRole } from '../../constants/roles';
import './Navbar.css';

export function Navbar() {
    const { user, userInfo, isLoading, login, logout } = useAuth();
    const { theme, toggleTheme } = useTheme();
    const { cartCount } = useCart();
    const navigate = useNavigate();

    return (
        <nav className="navbar">
            <div className="navbar__brand" onClick={() => navigate('/')} style={{ cursor: 'pointer' }}>
                <span className="navbar__logo">🥭</span>
                <span className="navbar__name">Mango Store</span>
            </div>
            <div className="navbar__actions">
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
                            <Link to="/cart" className="navbar__cart" style={{ cursor: 'pointer', position: 'relative', marginRight: '20px', textDecoration: 'none' }}>
                                <span style={{ fontSize: '1.4rem' }}>🛒</span>
                                {cartCount > 0 && (
                                    <span className="navbar__cart-count">{cartCount}</span>
                                )}
                            </Link>
                            <Link to="/orders" className="navbar__link" style={{ marginRight: '15px' }}>
                                My Orders
                            </Link>
                            {hasRole(userInfo?.role, Roles.Admin) && (
                                <Link to="/admin/products" className="navbar__link" style={{ marginRight: '15px' }}>
                                    ⚙️ Manage Products
                                </Link>
                            )}
                            <span className="navbar__user-name">
                                {userInfo?.given_name || userInfo?.name || user.profile.name || user.profile.email || 'User'}
                            </span>
                            <button className="navbar__btn navbar__btn--logout" onClick={logout}>
                                Sign out
                            </button>
                        </div>
                    ) : (
                        <button className="navbar__btn navbar__btn--login" onClick={login}>
                            Sign in
                        </button>
                    )
                )}
            </div>
        </nav>
    );
}
