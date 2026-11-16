---
name: fix-warnings
description: Use when the user runs /fix-warnings to clear .NET build warnings (WarningsAsErrors is on) with modern C# 15 fixes.
disable-model-invocation: true
---

# Fix Warnings

`WarningsAsErrors` is on in `Directory.Build.props`, so any warning is a build failure. Scan,
categorize and fix with modern C# 15 idioms.

## Workflow

1. **Scan:** `dotnet build MangoAspire.sln /warnaserror:false` to capture every warning.
2. **Categorize:** Nullable (CS86xx), Modernization (e.g. primary constructors, the `field`
   keyword, collection expressions) and Deprecation. Show a table of counts per project and category.
3. **Select:** the user picks projects or categories (default: all).
4. **Fix:** apply C# 15 syntax and explicit, fail-fast null checks. Never suppress a warning with a
   `#pragma` or `!` unless the user agrees.
5. **Verify:** `dotnet build MangoAspire.sln` must be clean, and `dotnet test MangoAspire.sln`
   (or the affected `*.Tests` projects) must pass.
6. **Hand off:** suggest the `generate-commit` skill.
