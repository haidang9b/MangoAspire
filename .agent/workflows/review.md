---
description: Triggers the automated code review and facilitates modern C# 14 and React 19 fixes.
---

# Workflow: review
Description: Triggers the automated code review and facilitates modern C# 14 and React 19 fixes.

## Execution Steps
1. **Analyze:** Execute the 'review-code' skill on the current changes.
2. **Report:** Display findings categorized by Backend (Onion Architecture/C# 14) and Frontend (Context/React Query).
3. **Fix Loop:** Ask the user if they want automated refactoring for modernization suggestions (like Primary Constructors).
4. **Verify:** Run `dotnet build` and `pnpm run lint`.