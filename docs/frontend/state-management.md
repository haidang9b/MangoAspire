# Frontend State Management

The `mango-ui` project purposefully avoids heavy monolithic global state management libraries (e.g., Redux Toolkit) in favor of the **React Context API** backed by custom hooks.

## Key Context Providers
Our state is broadly distributed into specific concern providers wrapped around the root `App.tsx`:
- **`AuthProvider`**: Manages the OpenID Connect (OIDC) authentication process and user claims.
- **`CartProvider`**: Manages the user's localized state for their shopping cart.
- **`ThemeProvider`**: Globally toggles color schemes (e.g., Light vs Dark mode).

## Custom API Hooks
The UI heavily utilizes custom abstract hooks (`useApi`, `useFetch`) to decouple component UI rendering from asynchronous data fetching logic.

- **`useFetch<T>`**: An internal hook abstracting loading flags, error boundaries, and standard data resolution.
- **Sub-Hooks**: e.g., `useProductsSearchParams()` - Encapsulates complex search, filter, and pagination routing parameters so the UI doesn't contain boilerplate logic.

## Routing and Protection
React Router routes are protected by higher-order components such as `<ProtectedRoute>` or `<AdminRoute>`. If user states managed by the AuthProvider indicate an unauthorized role, the user is cleanly redirected.
