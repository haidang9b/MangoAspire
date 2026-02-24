import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../hooks';
import { Roles, hasRole } from '../constants/roles';

interface Props {
    children: ReactNode;
}

export function AdminRoute({ children }: Props) {
    const { user, userInfo, isLoading, login } = useAuth();

    if (isLoading || (user && !userInfo)) {
        return (
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh' }}>
                <p style={{ color: '#a6adc8', fontFamily: 'Inter, sans-serif' }}>Loading…</p>
            </div>
        );
    }

    if (!user) {
        login();
        return null;
    }

    if (!hasRole(userInfo?.role, Roles.Admin)) {
        return <Navigate to="/" replace />;
    }

    return <>{children}</>;
}
