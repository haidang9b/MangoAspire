---
name: analyze-codebase
description: Analyze a .NET codebase to understand its architecture, identify tech stack variations, map out core components, and evaluate project health. Use when onboarding to a new project or planning large refactoring efforts.
---

# Analyze Codebase

This skill enables an agent to systematically analyze a .NET repository, identifying the architecture, technology stack, project relationships, and overall codebase structure. This is critical when first engaging with a new project or gathering context for cross-cutting changes.

## When to Use

- Initially exploring a large or unfamiliar .NET codebase.
- Extracting the architecture or mapping dependencies between projects (`.csproj`).
- Identifying the technology stack (e.g., ASP.NET Core, EF Core, WinForms, MAUI, etc.).
- Getting a high-level overview of solution structure before proposing architectural changes.

## When Not to Use

- Reviewing a single file or a small PR (use `code-review` instead).
- Diagnosing a specific build error or performance bottleneck (use MSBuild or diagnostics skills).

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Repository/Solution Path | No | The root directory or solution (`.sln`) file to start analysis from. Defaults to workspace root. |

## Workflow

### Step 1: Solutions and Projects Discovery

Scan the root directory for `.sln` and `.slnx` files. Understand the solution structure. List all `.csproj`, `.fsproj`, and `.vbproj` files to ascertain project boundaries. Look for `Directory.Build.props` or `Directory.Packages.props` for repository-wide MSBuild configurations.

### Step 2: Tech Stack Taxonomy

Analyze the project files to identify:
- **Target Frameworks**: Look at `<TargetFramework>` or `<TargetFrameworks>` (e.g., net8.0, netstandard2.0).
- **Project Types**: Identify if projects are Web SDK, Console, desktop (WPF/WinForms), or class libraries.
- **Key Dependencies**: Extract package references (e.g., `Microsoft.AspNetCore.App`, `Microsoft.EntityFrameworkCore`, `Newtonsoft.Json`).

### Step 3: Architecture and Layering

Identify the high-level architecture pattern (e.g., Clean Architecture, N-Tier, Microservices).
- Locate API/Web entry points (`Program.cs`, `Startup.cs`).
- Identify domain layers, infrastructure layers, and shared utilities.
- Map out the project-to-project reference dependencies (`<ProjectReference>`).

### Step 4: Core Conventions and Configuration

Review configuration files such as `appsettings.json`, `global.json`, `nuget.config`, and `.editorconfig`. Look for how dependency injection, logging, and middlewares are set up.

### Step 5: Test Infrastructure

Identify testing frameworks being used (xUnit, NUnit, MSTest) and where tests are located (e.g., `tests/`, `*Tests.csproj`). Assess the proportion of unit tests vs integration tests.

### Step 6: Documentation Generation

Write the complete analysis into a comprehensive markdown document named `Codebase-Architecture.md` (or similar) at the root of the workspace. The document should include sections for Architecture, Stack, Project References, and Entry Points.

## Validation

- [ ] A summary report of the architecture and stack is generated and saved as a markdown documentation file.
- [ ] Core projects and their purposes are clearly identified.
- [ ] Technology framework versions and significant NuGet packages are listed.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Getting stuck scanning `bin`/`obj` folders | Ensure the search specifically excludes output directories and focus on metadata first (`.csproj`, `.sln`). |
| Missing central package management | Always check `Directory.Packages.props` as versions might not be in the `.csproj`. |
| Over-analyzing every single file | Stick to top-level folder structures, project files, and application entry points to form the initial overview. |
