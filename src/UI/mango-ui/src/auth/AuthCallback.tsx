import { useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { userManager } from './authConfig';

/**
 * Rendered at /callback after the identity server redirects back.
 * Calls signinRedirectCallback(), then redirects to /.
 */
export function AuthCallback() {
    const handled = useRef(false);
    const navigate = useNavigate();

    useEffect(() => {
        if (handled.current) return;
        handled.current = true;

        userManager
            .signinRedirectCallback()
            .then(() => {
                navigate('/');
            })
            .catch((err) => {
                console.error('OIDC callback error:', err);
                navigate('/');
            });
    }, [navigate]);

    return (
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh' }}>
            <p style={{ color: '#a6adc8', fontFamily: 'Inter, sans-serif' }}>Signing in…</p>
        </div>
    );
}
