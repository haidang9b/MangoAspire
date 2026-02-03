# Coding Conventions & Standards

This document outlines the coding standards to ensure consistency across all microservices in the MangoAspire solution.

## Framework & Language
- Target **.NET 10**.
- Use **C# 14** features including:
  - **Extension blocks** for route mapping
  - **Primary Constructors** for Dependency Injection
  - **File-scoped namespaces** to reduce nesting
- Enable and respect **Nullable Reference Types**.
- Use `required` keyword for mandatory init-only properties.

## Program.cs Pattern
- Keep `Program.cs` ultra-clean (< 15 lines).
- Use extension methods: `AddApiDefaults()` and `UseApiPipeline()`.
- Call `MigrateDatabaseAsync()` for database migrations.

## Validation & Requests
- All **Commands** must have an associated `Validator` class inheriting from `AbstractValidator<T>`.
- Use **FluentValidation** for all input validation.
- Validation logic should be part of the feature slice.
- Complex business rules can still be validated in the Handler.

## API Response Standard
- All API endpoints must return a `ResultModel<T>` (defined in `Mango.Core`).
- Consistently use `Results.Ok(result)` or appropriate Minimal API result methods.
- For POST endpoints creating resources, use `Results.Created(uri, result)`.

## Naming Conventions
- **Handlers**: Use internal classes nested within feature classes.
- **Commands/Queries**: Use descriptive names like `GetProductById` or `CreateCart`.
- **Database Tables**: Use plural names (e.g., `Products`, `CartHeaders`).
- **API Routes**: Follow **RESTful** principles. Expose **resources** (nouns), not actions (verbs). Use **kebab-case** for all route segments.
  - *Correct*: `GET /api/products/{id}`, `POST /api/products`, `DELETE /api/products/{id}`
  - *Incorrect*: `GET /api/product/get-product/{id}`, `POST /api/product/add-product`
- **Extension Methods**: Use `AddApiDefaults()`, `UseApiPipeline()`, `MigrateDatabaseAsync()`.

## Error Handling
- Use the central `GlobalExceptionHandler` implementing `IExceptionHandler`.
- Register with `services.AddExceptionHandler<GlobalExceptionHandler>()` and `services.AddProblemDetails()`.
- **Null Checks**: Standardize null checks after finding records using `?? throw new DataVerificationException("Message");`.
- **Validation**: All business validation and data integrity checks must throw `DataVerificationException`.
- **No ResultModel.Error**: Do not return `ResultModel.Error` in handlers; the global exception handler will catch exceptions and format responses automatically.
- Use exceptions only for truly exceptional circumstances outside of data validation.

## Data Access
- **No Shared Logic**: Features should not depend on other features' handlers or logic.
- **Use EF Projections**: Always use `.Select(x => new Dto { ... })` in EF queries to project directly to DTOs for read queries. Do not fetch entire entities unless performing updates or deletes.
- **AsNoTracking**: Always use `.AsNoTracking()` for read-only queries to improve performance.
- **Performance Interceptors**: Use `PerformanceInterceptor` for monitoring slow queries.
- **Explicit Transactions**: Use explicit transactions only when multiple `SaveChangesAsync` calls are required or when involving external systems.

## MediatR Behaviors
Register behaviors in this order for the pipeline:
1. `LoggingBehavior<,>` - Request/response logging
2. `ValidationBehavior<,>` - FluentValidation integration
3. `TxBehavior<,>` - Transaction management

## Configuration & Dependencies
- Use **Central Package Management** via `Directory.Packages.props`.
- Connection strings and secrets should be managed via Environment variables or `appsettings.json` (for local dev).
- Reference `Mango.ServiceDefaults` for common observability and resiliency settings.
- Use `.NET Aspire` integrations:
  - `builder.AddNpgsqlDataSource()` for PostgreSQL
  - `builder.EnrichNpgsqlDbContext<T>()` for DbContext enrichment

## Route Definition (C# 14)
Use extension blocks for route mapping:
```csharp
public static class MyEndpoints
{
    extension(WebApplication app)
    {
        public RouteGroupBuilder MapMyApi()
        {
            var group = app.MapGroup("/api/my-resource").WithTags("MyResource");
            // Define routes here
            return group;
        }
    }
}
```
