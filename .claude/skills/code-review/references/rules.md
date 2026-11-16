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

## 2. Architecture & Design
**Repo guardrails (Critical):** a violation of an `AGENTS.md` guardrail blocks the merge. Rank it
**Critical**, the highest severity in the report:
- a repository or service layer wrapping the `DbContext` in a feature slice (only `Mango.Orchestrators` keeps one, for its saga);
- Clean, Onion or N-tier layers instead of Vertical Slice folders (`Features/` + `Routes/`);
- one slice calling another slice's handler;
- a command without a FluentValidation validator;
- business logic or wiring beyond `AddApiDefaults()` / `UseApiPipeline()` / `MigrateDatabaseAsync()` in `Program.cs`.

**Backend design (Warning):**
- **Separation of Concerns**: Business logic belongs in the feature slice's handler, not in Minimal API endpoints, `Program.cs` or UI components.
- **Dependency Injection**: Classes should receive their dependencies via constructor injection.
- **SOLID Principles**: Flag violations of Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion.

**Frontend design (Warning):**
- **Component Reusability**: Ensure React components are small, focused, and reusable. Avoid massive monolithic components.
- **State Management**: Keep state at the lowest level that needs it, and use React Context (as `src/UI/mango-ui/src/context/` does) to avoid prop drilling. Don't recommend adding a state-management library; the SPA has none.
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
