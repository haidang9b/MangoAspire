import type { ReactNode } from 'react';
import { AuthProvider } from '@/auth';
import { CartProvider } from '@/context/CartContext';
import { ThemeProvider } from '@/context/ThemeContext';

interface AppProvidersProps {
    children: ReactNode;
}

/**
 * Composes all global context providers.
 * Order matters: ThemeProvider → AuthProvider → CartProvider
 * (CartProvider depends on Auth, Auth is theme-agnostic)
 */
export function AppProviders({ children }: AppProvidersProps) {
    return (
        <ThemeProvider>
            <AuthProvider>
                <CartProvider>
                    {children}
                </CartProvider>
            </AuthProvider>
        </ThemeProvider>
    );
}
