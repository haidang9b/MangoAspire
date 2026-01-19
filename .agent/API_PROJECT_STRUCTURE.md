# API Project Structure & Architecture

All API projects in this solution must follow the **Vertical Slice Architecture (VSA)**. This approach organizes code by features rather than technical layers (like Controllers and Repositories).

## Directory Structure

```text
Service.API/
├── Data/               # DbContext and DB-related configurations
├── Entities/           # Database entities
├── Dtos/               # Data Transfer Objects
├── Extensions/         # IServiceCollection and WebApplication extensions
├── Features/           # Vertical Slices
│   └── [FeatureName]/  # Feature folder (e.g., Products, Carts)
│       ├── Feature1.cs # Command/Query + Handler
│       └── Feature2.cs
├── Routes/             # Minimal API route definitions
└── Program.cs          # Entry point
```

## Core Principles

1.  **Vertical Slices**: Each feature should be self-contained. Avoid sharing business logic between features unless it's strictly infrastructure-related.
2.  **No Repository Pattern**: Use the `DbContext` directly within the MediatR Handlers. This reduces boilerplate and makes the queries easier to optimize per feature.
3.  **Minimal APIs**: Use `app.MapGroup(...)` in `Routes/` to define endpoints. Avoid traditional MVC Controllers.
4.  **MediatR**: Use MediatR for all business logic. Controllers/Routes should only send Commands/Queries to MediatR.

## Implementation Pattern

### Feature Slice Template (.NET 10 / C# 14)
```csharp
namespace Service.API.Features.FeatureName;

public class FeatureAction
{
    public class Command : ICommand<ReturnDto>
    {
        public required string Prop { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Prop).NotEmpty();
        }
    }

    internal class Handler(MyDbContext dbContext) : IRequestHandler<Command, ResultModel<ReturnDto>>
    {
        public async Task<ResultModel<ReturnDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var dto = await dbContext.Entities
                .AsNoTracking()
                .Where(x => x.Id == request.Id)
                .Select(x => new ReturnDto 
                {
                    // Map properties here
                })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new DataVerificationException("Entity not found");

            return ResultModel<ReturnDto>.Create(dto);
        }
    }
}
```

### Route Mapping
```csharp
public static class FeatureEndpoints
{
    public static RouteGroupBuilder MapFeatureApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/feature").WithTags("Feature");
        group.MapGet("/", async (ISender sender) => Results.Ok(await sender.Send(new GetFeature.Query())));
        return group;
    }
}
```
## Dependency Injection & Middleware Configuration

To keep `Program.cs` clean and consistent across all services, use extension methods to group service registrations and pipeline configuration.

### Extension Patterns

#### 1. `WebApplicationBuilderExtensions.cs`
Used for high-level builder configurations like Service Defaults, Data Sources, and service groups.
```csharp
public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.AddNpgsqlDataSource(connectionName: "mydb");
        builder.Services.AddServices(builder.Configuration);
        builder.EnrichNpgsqlDbContext<MyDbContext>();
        return builder;
    }
}
```

#### 2. `IServiceCollectionExtensions.cs`
Used for specific `IServiceCollection` registrations like MediatR, Validators, and DbContext details.
```csharp
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddPostgresDbContext<MyDbContext>(...);
        services.AddMediatR(...);
        services.AddValidatorsFromAssembly(...);
        return services;
    }
}
```

#### 3. `WebApplicationExtensions.cs`
Used for configuring the request pipeline and asynchronous initialization tasks.
```csharp
public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment()) { app.UseSwagger(); ... }
        app.MapDefaultEndpoints();
        app.MapMyApi();
        return app;
    }

    public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app) { ... }
}
```
