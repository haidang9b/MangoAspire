# Code Review Rules

When performing a code review, analyze the provided C#/.NET backend and Frontend (CSHTML, HTML, JS, React) code against the following rigorous rules and guidelines. Provide actionable feedback when these rules are violated.

## 1. Security (Critical)
**Backend (.NET):**
- **No Hardcoded Secrets**: Reject any code containing hardcoded passwords, API keys, connection strings, or tokens.
- **Injection Prevention**: Ensure all database queries use parameterized commands (EF Core LINQ, Dapper parameters).
- **Authentication & Authorization**: Verify that sensitive endpoints and methods are protected with appropriate authorization attributes (e.g., `[Authorize]`).
- **Data Protection**: Ensure sensitive PII/PHI is not logged in plaintext.

**Frontend (React/JS/CSHTML/HTML):**
- **XSS Prevention**: Ensure UI outputs are properly encoded to prevent Cross-Site Scripting (XSS). Avoid `dangerouslySetInnerHTML` in React unless absolutely necessary and sanitized.
- **CSRF Protection**: Verify anti-forgery tokens are used where appropriate, especially in CSHTML forms (`@Html.AntiForgeryToken()`).
- **Sensitive Data in Browser**: Prevent storage of sensitive tokens, PII, or secrets in `localStorage` or `sessionStorage` in plaintext.

## 2. Architecture & Design (Warning)
**Backend (.NET):**
- **Separation of Concerns**: Business logic should not be within API Controllers, Minimal API endpoints, or UI components. It belongs in the Application/Domain layers.
- **Dependency Injection**: Classes should receive their dependencies via constructor injection.
- **SOLID Principles**: Flag violations of Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion.

**Frontend (React/JS/CSHTML/HTML):**
- **Component Reusability**: Ensure React components are small, focused, and reusable. Avoid massive monolithic components.
- **State Management**: Verify that state is managed at the appropriate level. Avoid prop drilling by using Context API or state management libraries when necessary.
- **Separation of Logic and UI**: Keep complex business logic out of UI components; utilize custom hooks or utility files.

## 3. Performance & Efficiency (Warning)
**Backend (.NET):**
- **Asynchronous Programming**: I/O-bound operations must use `async/await`. Avoid `Task.Result` or `Task.Wait()`.
- **Avoid Unnecessary Allocations**: Prefer `Span<T>` and `Memory<T>` for high-performance string/array manipulation.
- **Memory Leaks**: Ensure all objects implementing `IDisposable` are properly disposed of.

**Frontend (React/JS/CSHTML/HTML):**
- **Re-rendering Optimization**: Prevent unnecessary re-renders in React by using `useMemo`, `useCallback`, and `React.memo` where appropriate.
- **Bundle Size**: Advise on lazy loading components (`React.lazy`) and avoiding large, unnecessary dependencies.
- **DOM Manipulations**: In vanilla JS or jQuery, minimize direct DOM manipulations and reflows/repaints.

## 4. Quality & Idiomatic Usage (Suggestion)
**Backend (.NET):**
- **LINQ Usage**: Prefer LINQ for complex data manipulation.
- **Pattern Matching**: Encourage the use of modern C# pattern matching.
- **Null Reference Safety**: Ensure properly utilized Nullable Reference Types (`#nullable enable`).
- **Naming Conventions**: Classes/Methods/Properties should be `PascalCase`. Private fields `_camelCase`. Local variables `camelCase`.

**Frontend (React/JS/CSHTML/HTML):**
- **Modern JS**: Encourage ES6+ features (`const/let`, arrow functions, destructuring, optional chaining).
- **TypeScript (If applicable)**: Prefer explicit typing and avoiding `any`.
- **CSHTML Specific**: Avoid excessive C# logic directly inside Razor views; prefer using ViewModels and TagHelpers.
- **Naming Conventions**: React components should be `PascalCase`. Functions/variables `camelCase`.

## Output Formatting
When presenting feedback based on these rules, categorize them as:
1. **Critical**: Must fix before merge (e.g., Security, Data loss, Deadlocks).
2. **Warning**: Should address (e.g., Architectural flaws, Performance issues).
3. **Suggestion**: Nice to have (e.g., Modern syntax, Readability improvements).
