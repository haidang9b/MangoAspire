---
description: Scans the .NET solution for warnings and applies modern C# 14 fixes.
---

# Workflow: fix-warnings
Description: Scans the .NET solution for warnings and applies modern C# 14 fixes.

## Execution Steps

### Step 1: Scan and Categorize
1. Build: Run 'dotnet build /warnaserror:false' to capture all current warnings.
2. Analyze: Group warnings into Nullable (CS86xx), Modernization (Primary Constructors), and Deprecation.
3. Report: Display a summary table of warning counts.

### Step 2: Proposed Fixes
1. Selection: User targets specific projects or categories.
2. Drafting: Apply C# 14 syntax (field keyword, Primary Constructors) and proper null checks.

### Step 3: Verification
1. Rebuild: Ensure the build is clean.
2. Test: Run 'dotnet test' to verify business logic.
3. Commit: Trigger the generate-commit skill.