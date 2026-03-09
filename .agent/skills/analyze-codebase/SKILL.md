---
name: onboarding-analyzer
description: Analyzes the .NET 10 backend and React frontend to provide a technical map for a new developer.
---

# Logic Flow
1. **Identify Entry Points:** - Backend: Locate `Program.cs` and main Controllers.
   - Frontend: Locate `App.tsx`, `main.tsx`, and `routes.tsx`.

2. **Map the Data Flow:**
   - Trace a single "Feature" from a React Component -> API Call (Axios/Fetch) -> .NET Controller -> Service Layer -> EF Core Entity.

3. **Identify Tech Stack Details:**
   - Detect C# 14 features used (e.g., primary constructors).
   - Detect React state management (Zustand, Redux, or React Query).
   - Identify the database provider (PostgreSQL, SQL Server).

4. **Summarize Patterns:**
   - What is the error handling strategy? (e.g., Result Pattern, Global Exception Middleware).
   - What is the testing strategy? (xUnit).

# Output Format
- **Architecture Overview:** High-level summary of the folder structure.
- **Key Files:** 5 most important files to read first.
- **Onboarding To-Do:** A list of 3-5 tasks for the user to "poke" the code and learn.