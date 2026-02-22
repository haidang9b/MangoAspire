import type { ReactNode } from 'react';
import { useAuth } from './AuthContext';

interface Props {
    children: ReactNode;
}

export function ProtectedRoute({ children }: Props) {
    const { user, isLoading, login } = useAuth();

    if (isLoading) {
        return (
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh' }}>
                <p style={{ color: '#a6adc8', fontFamily: 'Inter, sans-serif' }}>Loading…</p>
            </div>
        );
    }

    if (!user) {
        // Trigger login redirect immediately
        login();
        return null;
    }

    return <>{children}</>;
}
