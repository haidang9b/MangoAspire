import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { AuthCallback } from './auth/AuthCallback';
import { SilentCallback } from './auth/SilentCallback';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { AdminRoute } from './auth/AdminRoute';
import { CartProvider } from './context/CartContext';
import { Navbar } from './components/Navbar';
import { ChatPopup } from './components/ChatPopup';
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
                  <Route path="/callback" element={<AuthCallback />} />
                  <Route path="/silent-callback" element={<SilentCallback />} />
                  <Route path="/" element={<ProductsPage />} />
                  <Route
                    path="/cart"
                    element={
                      <ProtectedRoute>
                        <CartPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route path="/product/:id" element={<ProductDetailsPage />} />
                  <Route
                    path="/checkout"
                    element={
                      <ProtectedRoute>
                        <CheckoutPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="/confirmation"
                    element={
                      <ProtectedRoute>
                        <ConfirmationPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="/orders"
                    element={
                      <ProtectedRoute>
                        <OrdersPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="/orders/:id"
                    element={
                      <ProtectedRoute>
                        <OrderDetailsPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="/admin/products"
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
