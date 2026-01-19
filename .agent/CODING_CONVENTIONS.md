# Coding Conventions & Standards

This document outines the coding standards to ensures consistency across all microservices in the MangoAspire solution.

## Framework & Language
- Target **.NET 10**.
- Use **C# 14** features.
- Prefer **Primary Constructors** for Dependency Injection.
- Use **File-scoped namespaces** to reduce nesting.
- Enable and respect **Nullable Reference Types**.

## Validation & Requests
- All **Commands** must have an associated `Validator` class inheriting from `AbstractValidator<T>`.
- Use **FluentValidation** for all input validation.
- Validation logic should be part of the feature slice.
- Complex business rules can still be validated in the Handler.

## API Response Standard
- All API endpoints must return a `ResultModel<T>` (defined in `Mango.Core`).
- Consistently use `Results.Ok(result)` or appropriate Minimal API result methods.

## Naming Conventions
- **Handlers**: Use internal classes for handlers within feature classes.
- **Commands/Queries**: Use descriptive names like `GetProductById` or `CreateCart`.
- **Database Tables**: Use plural names (e.g., `Products`, `CartHeaders`).
- **API Routes**: Follow **RESTful** principles. Expose **resources** (nouns), not actions (verbs). Use **kebab-case** for all route segments.
  - *Correct*: `GET /api/carts/{userId}`, `POST /api/carts`, `DELETE /api/carts/{cartDetailsId}`
  - *Incorrect*: `GET /api/cart/get-cart/{userId}`, `POST /api/cart/add-cart`
- **Dependency Injection**: Use extension methods named `AddApiDefaults()` and `UseApiPipeline()` in `Extensions/` to keep `Program.cs` clean.

## Error Handling
- Use the central `GlobalExceptionHandler` and `ProblemDetails` middleware.
- **Null Checks**: Standardize null checks after finding records using `?? throw new DataVerificationException("Message");`.
- **Validation**: All business validation and data integrity checks must throw `DataVerificationException`.
- **No ResultModel.Error**: Do not return `ResultModel.Error` in handlers; the global exception handler will catch `DataVerificationException` and format it as a `ProblemDetails` or `ResultModel` error response automatically.
- Use exceptions only for truly exceptional circumstances outside of data validation.

## Data Access
- **No Shared Logic**: Features should not depend on other features' handlers or logic.
- **Use EF Projections**: Always use `.Select(x => new Dto { ... })` in EF queries to project directly to DTOs for read queries. Do not fetch entire entities unless performing updates or deletes.
- **AsNoTracking**: Always use `.AsNoTracking()` for read-only queries to improve performance.
- **Explicit Transactions**: Use explicit transactions only when multiple `SaveChangesAsync` calls are required or when involving external systems.

## Configuration & Dependencies
- Use **Central Package Management** via `Directory.Packages.props`.
- Connections strings and secrets should be managed via Environment variables or `appsettings.json` (for local dev).
- Reference `Mango.ServiceDefaults` for common observability and resiliency settings.
