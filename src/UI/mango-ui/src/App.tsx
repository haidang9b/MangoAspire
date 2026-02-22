import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { AuthCallback } from './auth/AuthCallback';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { CartProvider } from './context/CartContext';
import { Navbar } from './components/Navbar';
import { ChatPopup } from './components/ChatPopup';
import { ProductsPage } from './pages/ProductsPage';
import { CartPage } from './pages/CartPage';
import { ProductDetailsPage } from './pages/ProductDetailsPage';
import { CheckoutPage } from './pages/CheckoutPage';
import { ConfirmationPage } from './pages/ConfirmationPage';
import OrdersPage from './pages/OrdersPage';
import OrderDetailsPage from './pages/OrderDetailsPage';
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
