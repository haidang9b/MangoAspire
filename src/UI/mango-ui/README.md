# 🥭 Mango Store SPA

A premium, feature-rich React application built for the MangoAspire microservices ecosystem. This application provides a modern shopping experience with a focus on speed, aesthetics, and real-time AI assistance.

## 🚀 Features

- **🛍️ Product Catalog**: Browse products with category filtering and real-time search.
- **📑 Product Details**: Deep-dive into product specifications and availability.
- **🛒 Shopping Cart**: Persistent cart management with coupon code support.
- **💳 Checkout Flow**: Streamlined multi-step checkout with address validation.
- **🤖 AI Shopping Assistant**: Real-time streaming chat assistant to help with your shopping journey.
- **📋 Order History**: View past orders and detailed status tracking.
- **🌗 Theme System**: Beautiful Dark and Light modes with persistent user preference.
- **🔐 Secure Auth**: Fully integrated with Identity Server (OpenIddict) for secure OIDC authentication.

## 🛠️ Tech Stack

- **Frontend Framework**: [React 18](https://reactjs.org/)
- **Build Tool**: [Vite](https://vitejs.dev/)
- **Language**: [TypeScript](https://www.typescriptlang.org/)
- **Styling**: Vanilla CSS (CSS Variables) with Modern Design Principles.
- **API Client**: [Axios](https://axios-http.com/) & Native Fetch (for Streaming).
- **Authentication**: [oidc-client-ts](https://github.com/authts/oidc-client-ts).
- **Icons/Emoji**: Rich emoji-based iconography for a friendly UI.

## ⚙️ Configuration

The app uses environment variables defined in `.env`:

- `VITE_GATEWAY_URL`: Base URL for the YARP Gateway (default: `https://localhost:7000`).
- `VITE_IDENTITY_URL`: URL for the Identity Server.
- `VITE_CHAT_URL`: Dedicated endpoint for the Chat service via Gateway.

## 🛠️ Getting Started

### Prerequisites

- Node.js (v18+)
- pnpm (recommended) or npm

### Installation

```bash
# Clone the repository
git clone <repo-url>

# Navigate to the UI directory
cd src/UI/mango-ui

# Install dependencies
pnpm install
```

### Development

```bash
# Run the development server
pnpm dev
```

The application will be available at `http://localhost:5173`.

## 📂 Documentation

- [Architecture Overview](./ARCHITECTURE.md) - Deep dive into the project's technical design.
- [Walkthrough](../../../C:/Users/Dang/.gemini/antigravity/brain/08601a18-7e4e-4514-90f8-6ffea8cfe43e/walkthrough.md) - Summary of implemented features and verification.
