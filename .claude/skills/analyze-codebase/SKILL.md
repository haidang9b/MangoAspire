---
name: analyze-codebase
description: Use when onboarding to an unfamiliar area of this .NET solution, or before a cross-cutting change or large refactor, and the project/dependency structure and tech stack need mapping out first.
---

# Analyze Codebase

Map the architecture, tech stack, and project relationships of a .NET repository. Use when first
engaging with a new area or gathering context for a cross-cutting change — not for reviewing a
single file/PR (`code-review`) or diagnosing one build error.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Repository/Solution path | No | Root directory or `.sln`. Defaults to workspace root. |

## Workflow

1. **Discovery:** find `.sln`/`.slnx`, list `.csproj` files, check `Directory.Build.props` /
   `Directory.Packages.props` for repo-wide MSBuild config.
2. **Tech stack:** `<TargetFramework>`, project types (Web SDK, Console, class library), key
   package references.
3. **Architecture:** identify the pattern in use (in this repo: **Vertical Slice Architecture** by
   feature — see `AGENTS.md` and `.agent/rules/architecture.md`; not a layered N-Tier split). Locate
   entry points (`Program.cs`), map `<ProjectReference>` dependencies.
4. **Conventions:** `appsettings.json`, `global.json`, `.editorconfig`; how DI/logging/middleware
   are wired.
5. **Tests:** framework in use (this repo: xUnit + Moq + Shouldly) and where tests live.
6. **Report:** write findings to a markdown doc (Architecture, Stack, Project References, Entry
   Points).

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Scanning `bin`/`obj` | Exclude output directories; start from `.csproj`/`.sln` metadata. |
| Missing central package management | Check `Directory.Packages.props` — versions may not be in the `.csproj`. |
| Over-analyzing every file | Stick to top-level structure, project files, and entry points. |
