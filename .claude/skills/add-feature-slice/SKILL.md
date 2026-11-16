---
name: add-feature-slice
description: Use when adding a new API project, feature slice, minimal-API route group, or integration event under src/Services and the exact Vertical Slice file layout and wiring templates are needed.
---

# Add Feature Slice

Scaffold Vertical Slice code that matches the existing services; `src/Services/Products.API` is the
reference. The request pipeline runs through the in-house mediator (`src/Shared/Mediator`), using
`ISender` (`SendAsync`) and handlers' `HandleAsync`.

## Checklist

1. **Slice:** `Features/<Area>/<FeatureName>.cs` holds the Query/Command class with a nested
   `Handler` that uses the `DbContext` directly (no repository) and returns `ResultModel<T>`. Reads
   use `AsNoTracking()` plus a `Select` projection to a DTO. → `references/feature-slice.md`
2. **Validator:** a FluentValidation `AbstractValidator<Command>` in the same file, for every command.
   Business-rule failures throw `DataVerificationException`.
3. **Route:** `Routes/<Area>Endpoints.cs`, a C# 14 extension block with a kebab-case REST group that
   calls `sender.SendAsync(...)` and maps the result to `Results.Ok` / `Results.Created`.
   → `references/feature-slice.md`
4. **Wiring:** keep `Program.cs` to `AddApiDefaults()` / `UseApiPipeline()` / `MigrateDatabaseAsync()`,
   and map the new group in `WebApplicationExtensions`. A new API project copies the three
   `Extensions/` files. → `references/program-wiring.md`
5. **Events** (cross-service only): publish through `IEventBus` (`PublishAsync`); handle with an
   `IIntegrationEventHandler<T>` in `IntegrationEvents/Handlers/` and register it with
   `.AddSubscription<TEvent, THandler>(...)`. → `references/integration-events.md`
6. **Verify:** `dotnet build` on the project, then add tests with the `create-unit-test` skill.

## Pitfalls

| Pitfall | Fix |
|---|---|
| A service or repository layer | Handler + `DbContext` only. |
| Calling another slice's handler | Share infrastructure only; no cross-slice calls. |
| Missing validator | Every command has one. |
| MediatR APIs (Send, Handle, AddMediatR) | This repo's mediator uses `SendAsync`, `HandleAsync` and `AddMediator`. |
