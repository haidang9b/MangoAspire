# API Project Structure & Architecture

All API projects in this solution must follow the **Vertical Slice Architecture (VSA)**. This approach organizes code by features rather than technical layers (like Controllers and Repositories).

## Directory Structure

```text
Service.API/
├── Data/               # DbContext and DB-related configurations
│   ├── EntityTypeConfigurations/  # EF Core entity configurations
│   └── Migrations/     # EF Core migrations
├── Entities/           # Database entities
├── Dtos/               # Data Transfer Objects
├── Extensions/         # IServiceCollection and WebApplication extensions
│   ├── IServiceCollectionExtensions.cs
│   ├── WebApplicationBuilderExtensions.cs
│   └── WebApplicationExtensions.cs
├── ExceptionHandlers/  # Custom exception handlers
├── Features/           # Vertical Slices
│   └── [FeatureName]/  # Feature folder (e.g., Products, Carts)
│       ├── Feature1.cs # Command/Query + Handler + Validator
│       └── Feature2.cs
├── Routes/             # Minimal API route definitions
└── Program.cs          # Entry point (ultra-clean)
```

## Core Principles

1.  **Vertical Slices**: Each feature should be self-contained. Avoid sharing business logic between features unless it's strictly infrastructure-related.
2.  **No Repository Pattern**: Use the `DbContext` directly within the MediatR Handlers. This reduces boilerplate and makes the queries easier to optimize per feature.
3.  **Minimal APIs**: Use `app.MapGroup(...)` in `Routes/` to define endpoints. Avoid traditional MVC Controllers.
4.  **MediatR**: Use MediatR for all business logic. Routes should only send Commands/Queries to MediatR.
5.  **Clean Program.cs**: The `Program.cs` should be minimal - just call extension methods.

## Implementation Patterns

### Ultra-Clean Program.cs
```csharp
using Products.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();
```

### Feature Slice Template (.NET 10 / C# 14)
```csharp
namespace Service.API.Features.FeatureName;

public class GetEntityById
{
    public class Query : IQuery<EntityDto>
    {
        public Guid EntityId { get; set; }

        internal class Handler(MyDbContext dbContext) : IRequestHandler<Query, ResultModel<EntityDto>>
        {
            public async Task<ResultModel<EntityDto>> Handle(Query request, CancellationToken cancellationToken)
            {
                var dto = await dbContext.Entities
                    .AsNoTracking()
                    .Where(x => x.Id == request.EntityId)
                    .Select(x => new EntityDto
                    {
                        // Map properties here
                    })
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new DataVerificationException("Entity not found");

                return ResultModel<EntityDto>.Create(dto);
            }
        }
    }
}
```

### Route Mapping (C# 14 Extension Blocks)
```csharp
using MediatR;
using Products.API.Features.Products;

namespace Products.API.Routes;

public static class ProductEndpoints
{
    extension(WebApplication app)
    {
        public RouteGroupBuilder MapProductsApi()
        {
            var group = app.MapGroup("/api/products")
                .WithTags("Products");

            group.MapGet("/", async (ISender sender) =>
            {
                var result = await sender.Send(new GetProducts.Query());
                return Results.Ok(result);
            });

            group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
            {
                var result = await sender.Send(new GetProductById.Query { ProductId = id });
                return Results.Ok(result);
            });

            group.MapPost("/", async (CreateProduct.Command command, ISender sender) =>
            {
                var result = await sender.Send(command);
                return Results.Created($"/api/products/{result.Data}", result);
            });

            return group;
        }
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
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        return builder;
    }
}
```

#### 2. `IServiceCollectionExtensions.cs`
Used for specific `IServiceCollection` registrations like MediatR, Validators, DbContext, and Exception Handlers.
```csharp
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();

        services.AddPostgresDbContext<MyDbContext>(
            configuration.GetConnectionString("mydb") 
                ?? throw new ArgumentNullException("mydb"),
            doMoreDbContextOptionsConfigure: (sp, options) =>
            {
                options.AddInterceptors(sp.GetRequiredService<PerformanceInterceptor>());
            });

        services.AddScoped<PerformanceInterceptor>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TxBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddDocumentApi("My API", "v1", "My API Description");

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
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();
        app.MapDefaultEndpoints();
        app.MapMyApi();

        return app;
    }

    public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        await dbContext.Database.MigrateAsync();
        return app;
    }
}
```

## MediatR Behaviors Pipeline

The following behaviors are registered in order:
1. **LoggingBehavior** - Logs request/response for observability
2. **ValidationBehavior** - Validates requests using FluentValidation
3. **TxBehavior** - Wraps commands in database transactions
