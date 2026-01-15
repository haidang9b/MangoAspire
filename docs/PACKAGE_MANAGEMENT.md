# Centralized NuGet Package Management Guide

## Overview

Your MangoAspire solution now uses **Central Package Management (CPM)**, a feature introduced in NuGet 6.2+ that allows you to manage all package versions in a single location.

## Files Involved

### 1. `Directory.Packages.props`
Located at the solution root, this file defines all package versions centrally.

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="PackageName" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### 2. `Directory.Build.props`
Contains solution-wide MSBuild properties like nullable reference types and warnings as errors.

## How It Works

### Before CPM (Decentralized)
Each `.csproj` file specified package versions:
```xml
<PackageReference Include="FluentValidation" Version="12.1.1" />
```

### After CPM (Centralized)
- **Directory.Packages.props**: Defines versions
  ```xml
  <PackageVersion Include="FluentValidation" Version="12.1.1" />
  ```
- **Project files (.csproj)**: Reference packages WITHOUT versions
  ```xml
  <PackageReference Include="FluentValidation" />
  ```

## Benefits

✅ **Single Source of Truth**: All package versions in one file  
✅ **Consistency**: Ensures all projects use the same package versions  
✅ **Easy Updates**: Update a version once, applies everywhere  
✅ **Reduced Merge Conflicts**: Fewer version conflicts across branches  
✅ **Better Visibility**: See all dependencies at a glance  

## Common Tasks

### Adding a New Package

1. **Add version to Directory.Packages.props**:
   ```xml
   <PackageVersion Include="Serilog" Version="3.1.1" />
   ```

2. **Reference in project file** (without version):
   ```xml
   <PackageReference Include="Serilog" />
   ```

### Updating a Package Version

Simply update the version in `Directory.Packages.props`:
```xml
<!-- Before -->
<PackageVersion Include="FluentValidation" Version="12.1.1" />

<!-- After -->
<PackageVersion Include="FluentValidation" Version="12.2.0" />
```

All projects using this package will automatically use the new version.

### Project-Specific Version Override

If a specific project needs a different version (rare), you can override:
```xml
<PackageReference Include="FluentValidation" VersionOverride="11.0.0" />
```

> [!WARNING]
> Avoid version overrides when possible. They defeat the purpose of centralized management.

### Checking for Package Updates

Use the .NET CLI to check for outdated packages:
```powershell
dotnet list package --outdated
```

To update packages interactively:
```powershell
dotnet outdated
```

> [!TIP]
> Install `dotnet-outdated-tool` globally for better update management:
> ```powershell
> dotnet tool install --global dotnet-outdated-tool
> ```

## Current Package Inventory

Your solution currently uses these package categories:

### Aspire (Orchestration)
- `Aspire.Hosting.AppHost` - 9.5.0
- `Aspire.Hosting.PostgreSQL` - 9.5.0
- `Aspire.Npgsql` - 13.1.0

### Entity Framework Core
- `EFCore.NamingConventions` - 10.0.0
- `Microsoft.EntityFrameworkCore` - 10.0.2
- `Microsoft.EntityFrameworkCore.Relational` - 10.0.1
- `Microsoft.EntityFrameworkCore.Tools` - 10.0.1
- `Npgsql.EntityFrameworkCore.PostgreSQL` - 10.0.0

### Validation & Mediator
- `FluentValidation` - 12.1.1
- `FluentValidation.DependencyInjectionExtensions` - 12.1.1
- `MediatR` - 14.0.0

### OpenTelemetry (Observability)
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` - 1.14.0
- `OpenTelemetry.Extensions.Hosting` - 1.14.0
- `OpenTelemetry.Instrumentation.AspNetCore` - 1.14.0
- `OpenTelemetry.Instrumentation.Http` - 1.14.0
- `OpenTelemetry.Instrumentation.Runtime` - 1.14.0

## Version Alignment Recommendations

> [!IMPORTANT]
> Some of your packages have version mismatches that should be aligned:

### Entity Framework Core Packages
Currently mixed versions (10.0.0, 10.0.1, 10.0.2). Recommend aligning all to **10.0.2**:
```xml
<PackageVersion Include="EFCore.NamingConventions" Version="10.0.2" />
<PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.2" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.2" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.2" />
<PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.2" />
```

### Aspire Packages
Mixed versions (9.5.0 and 13.1.0). Check compatibility and align if possible.

## Troubleshooting

### Error: "Package version cannot be specified"
If you see this error, it means you have a version in both places:
- **Fix**: Remove the `Version` attribute from the `.csproj` file

### Restore Fails After Migration
1. Clean the solution: `dotnet clean`
2. Delete `bin` and `obj` folders
3. Restore again: `dotnet restore`

### Package Not Found
Ensure the package is defined in `Directory.Packages.props` before referencing it in project files.

## Best Practices

1. **Keep versions aligned**: Use the same version for related packages (e.g., all EF Core packages)
2. **Regular updates**: Review and update packages monthly
3. **Test after updates**: Run tests after updating package versions
4. **Document breaking changes**: Comment major version updates in `Directory.Packages.props`
5. **Group logically**: Organize packages by category with comments

## Migration Checklist

✅ Created `Directory.Packages.props`  
✅ Enabled `ManagePackageVersionsCentrally`  
✅ Moved all package versions to central file  
✅ Removed version attributes from all `.csproj` files  
✅ Verified restore works: `dotnet restore`  
✅ Verified build works: `dotnet build`  

## Additional Resources

- [Microsoft Docs: Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [NuGet Blog: CPM Announcement](https://devblogs.microsoft.com/nuget/introducing-central-package-management/)
