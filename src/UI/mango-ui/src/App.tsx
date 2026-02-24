import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider, AuthCallback, SilentCallback, ProtectedRoute, AdminRoute } from './auth';
import { CartProvider } from './context/CartContext';
import { Navbar } from './components/Navbar';
import { ChatPopup } from './components/ChatPopup';
import { ROUTES } from './constants';
import {
  AdminProductsPage,
  ProductsPage,
  CartPage,
  ProductDetailsPage,
  CheckoutPage,
  ConfirmationPage,
  OrdersPage,
  OrderDetailsPage
} from './pages';
import { ThemeProvider } from './context/ThemeContext';
import './index.css';
import './App.css';

function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <CartProvider>
          <BrowserRouter>
            <div className="app">
              <Navbar />
              <main className="main-content">
                <Routes>
                  <Route path={ROUTES.CALLBACK} element={<AuthCallback />} />
                  <Route path={ROUTES.SILENT_CALLBACK} element={<SilentCallback />} />
                  <Route path={ROUTES.HOME} element={<ProductsPage />} />
                  <Route
                    path={ROUTES.CART}
                    element={
                      <ProtectedRoute>
                        <CartPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route path={ROUTES.PRODUCT_DETAILS} element={<ProductDetailsPage />} />
                  <Route
                    path={ROUTES.CHECKOUT}
                    element={
                      <ProtectedRoute>
                        <CheckoutPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path={ROUTES.CONFIRMATION}
                    element={
                      <ProtectedRoute>
                        <ConfirmationPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path={ROUTES.ORDERS}
                    element={
                      <ProtectedRoute>
                        <OrdersPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path={ROUTES.ORDER_DETAILS}
                    element={
                      <ProtectedRoute>
                        <OrderDetailsPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path={ROUTES.ADMIN_PRODUCTS}
                    element={
                      <AdminRoute>
                        <AdminProductsPage />
                      </AdminRoute>
                    }
                  />
                </Routes>
              </main>
              <ChatPopup />
            </div>
          </BrowserRouter>
        </CartProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;
