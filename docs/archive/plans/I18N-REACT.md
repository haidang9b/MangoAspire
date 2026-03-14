# Technical Blueprint: React i18n Support

## 📝 Business Summary
Add internationalization (i18n) support to the `mango-ui` React frontend to allow users to switch languages seamlessly.

## 🏗️ Technical Impact Map
- **Backend:** None.
- **Frontend:**
  - Install `i18next` and `react-i18next`.
  - Create `src/i18n.ts` to coordinate language detection/selection and initialize resources.
  - Create locale JSON files for English (`en`) and another language (e.g., Vietnamese `vi`) in a new `src/locales` directory.
  - Create `LanguageSelector.tsx` component.
  - Modify `src/components/Navbar/Navbar.tsx` to include the LanguageSelector.
  - Modify `src/main.tsx` to initialize i18n before the app mounts.
  - Integrate `useTranslation` hook into all UI components containing static text:
    - `components/ProductCard/ProductCard.tsx`, `components/SearchBox/SearchBox.tsx`, `components/ChatPopup/ChatPopup.tsx`, `components/Pagination/Pagination.tsx`
  - Integrate `useTranslation` hook into all page components containing static text:
    - `pages/ProductsPage/ProductsPage.tsx`, `pages/ProductDetailsPage/ProductDetailsPage.tsx`
    - `pages/CartPage/CartPage.tsx`, `pages/CheckoutPage/CheckoutPage.tsx`, `pages/ConfirmationPage/ConfirmationPage.tsx`
    - `pages/OrdersPage/OrdersPage.tsx`, `pages/OrderDetailsPage/OrderDetailsPage.tsx`
    - `pages/AdminProductsPage/AdminProductsPage.tsx`, `ProductForm.tsx`, `DeleteDialog.tsx`

## 🧪 Acceptance Criteria (AC)
- Given a user is on any page, when they click the language selector and choose a language, then the static text on the page updates immediately.
- The selected language choice should persist or fallback gracefully.

## ❓ Clarifying Questions
1. Should we default to English and Vietnamese? Are there specific language keys we should use?
2. Does the backend need to supply localized data, or are we just localizing static UI elements in this ticket?
