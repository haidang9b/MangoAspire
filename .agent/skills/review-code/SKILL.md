---
name: "Review: Full-Stack Code Review"
description: "Rigorous technical audit focusing on Onion Architecture, C# 14 modernization, and React 19 Context patterns."
---

# Skill: Code Reviewer

## Goal
Identify architectural violations and code-smells in the Overseas Order Management System.

## Logic Flow
1. **Scope Detection:** - Analyze staged changes via `git diff --cached` or specific file paths.
2. **Backend Audit (.NET 10):**
   - **Onion Architecture:** Ensure `OverseasOrder.Domain` has zero dependencies. Check that `Api` only speaks to `Application` via DTOs.
   - **C# 14 Modernization:** Flag traditional constructors that should be **Primary Constructors**. Identify where the `field` keyword can simplify properties.
   - **Result Pattern:** Verify services return a `Result<T>` instead of throwing exceptions for expected business logic failures.
3. **Frontend Audit (React 19):**
   - **Server State:** Ensure all API interactions use **React Query** (useQuery/useMutation).
   - **Global State:** Verify global concerns (Auth, Notifications) use the established **Provider Pattern** (`useContext`).
   - **Performance:** Flag heavy logic inside components that should be moved to custom hooks. Ensure `useContext` isn't causing massive re-renders in unrelated branches.
   - **Styling:** Validate strict adherence to **Tailwind CSS**.
4. **Testing Audit:** - Ensure new logic is covered by **xUnit (Shouldly)** or **Vitest**.

## Output Format
### Architectural Boundaries
- [Status] (e.g., Compliant / Violation)
- [Details]

### Implementation Suggestions
- **Backend:** Specific lines for C# 14 optimizations.
- **Frontend:** Component-level feedback on Hook usage and Context patterns.

### Quality Check
- Missing tests or build warnings identified.