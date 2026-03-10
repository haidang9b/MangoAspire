import { Routes, Route } from 'react-router-dom';
import { ProtectedRoute, AdminRoute, AuthCallback, SilentCallback } from '@/auth';
import {
    AdminProductsPage,
    ProductsPage,
    CartPage,
    ProductDetailsPage,
    CheckoutPage,
    ConfirmationPage,
    OrdersPage,
    OrderDetailsPage,
} from '@/pages';
import { ROUTES } from '@/constants';

export function AppRouter() {
    return (
        <Routes>
            {/* Auth callbacks */}
            <Route path={ROUTES.CALLBACK} element={<AuthCallback />} />
            <Route path={ROUTES.SILENT_CALLBACK} element={<SilentCallback />} />

            {/* Public routes */}
            <Route path={ROUTES.HOME} element={<ProductsPage />} />
            <Route path={ROUTES.PRODUCT_DETAILS} element={<ProductDetailsPage />} />

            {/* Protected — logged-in users */}
            <Route path={ROUTES.CART} element={<ProtectedRoute><CartPage /></ProtectedRoute>} />
            <Route path={ROUTES.CHECKOUT} element={<ProtectedRoute><CheckoutPage /></ProtectedRoute>} />
            <Route path={ROUTES.CONFIRMATION} element={<ProtectedRoute><ConfirmationPage /></ProtectedRoute>} />
            <Route path={ROUTES.ORDERS} element={<ProtectedRoute><OrdersPage /></ProtectedRoute>} />
            <Route path={ROUTES.ORDER_DETAILS} element={<ProtectedRoute><OrderDetailsPage /></ProtectedRoute>} />

            {/* Admin only */}
            <Route path={ROUTES.ADMIN_PRODUCTS} element={<AdminRoute><AdminProductsPage /></AdminRoute>} />
        </Routes>
    );
}
