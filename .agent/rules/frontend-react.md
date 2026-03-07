---
trigger: glob
globs: Only triggers for React/TypeScript files and the src/UImango-ui directory.
---

---
trigger: glob
globs: Only triggers for React/TypeScript files and the Frontend directory.
---

# Rule: React Frontend Standards
**Context:** Files in `src/UI/mango-ui/`

- **State Management:** Use `AuthContext` for user sessions and `NotificationContext` for alerts. Do not introduce new global states unless necessary.
- **Data Fetching:** Use the custom `useFetch` hook. Respect the **Stale-While-Revalidate** strategy.
- **UI:** Use **Material UI (MUI)** components. For tables, always use **MUI X DataGrid**.
- **Services:** Keep API calls in `src/UI/mango-ui/src/api/`. Components should only call these services through hooks.
- **TypeScript:** No `any`. Define interfaces for all API responses in a shared types folder or within the service file.