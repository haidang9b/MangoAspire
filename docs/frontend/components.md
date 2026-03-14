# Frontend Components

The front-end user interface is primarily powered by the **mango-ui** project, a modern React application utilizing TypeScript, Vite, and strict architectural standards.

## Core Refactoring Principles
Recent UI refactorings have extracted monolithic structures into distinct, cohesive components:
- `App.tsx` has been decomposed into smaller layers including `AppRoutes` and `AppProviders` to comply with SOLID principles.
- Complex component logic (e.g., forms, search bars) has been isolated.

## Shared Components
- `LanguageSelector`: Triggers global `i18next` language toggling, often utilizing the `useTranslation` hook, and is integrated into the Navbar.

## Theming and Styling
We have actively migrated away from third-party UI libraries like Material UI to favor custom, lightweight UI primitive components.

- **`styled-components`**: Used to build robust UI primitives like `<Button>` and `<Card>` without rigid external dependencies.
- **CSS Variables & Base CSS**: A standardized root CSS variable system manages consistent colors, typography, spacing, and transition speeds.
- **Dark Mode**: Automatically scales down the base CSS variables utilizing an active theme context provider.

## Internationalization (i18n)
All hardcoded strings inside views, modals, and lists have been successfully localized. We use unified translation mapping (en/vi) that toggles via an embedded Language hook/Context.

## Component File Structure
Typically, a feature UI component is isolated inside a folder. For example:
```text
components/
└── SearchBox/
    ├── SearchBox.tsx     // Structural UI Logic
    ├── SearchBox.css     // Isolated Styling / styled-components definition
    ├── index.tsx         // Standardized Export
```
