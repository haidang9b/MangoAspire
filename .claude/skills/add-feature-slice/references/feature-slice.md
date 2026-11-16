# Feature slice templates

Moved from the former API-project-structure convention doc and corrected against `src/Services/Products.API` (in-house mediator in `src/Shared/Mediator`, not MediatR).

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
├── IntegrationEvents/  # Integration Events and Handlers
│   ├── Events/         # Event definitions (records)
│   └── Handlers/       # Event Handlers implementation
├── Routes/             # Minimal API route definitions
└── Program.cs          # Entry point (ultra-clean)
```


## Feature Slice Template (.NET 11 / C# 15)
```csharp
namespace Service.API.Features.FeatureName;

public class GetEntityById
{
    public class Query : IQuery<EntityDto>
    {
        public Guid EntityId { get; set; }

        internal class Handler(MyDbContext dbContext) : IRequestHandler<Query, ResultModel<EntityDto>>
        {
            public async Task<ResultModel<EntityDto>> HandleAsync(Query request, CancellationToken cancellationToken)
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


## Route Mapping (C# 14 Extension Blocks)
```csharp
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
                var result = await sender.SendAsync(new GetProducts.Query());
                return Results.Ok(result);
            });

            group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
            {
                var result = await sender.SendAsync(new GetProductById.Query { ProductId = id });
                return Results.Ok(result);
            });

            group.MapPost("/", async (CreateProduct.Command command, ISender sender) =>
            {
                var result = await sender.SendAsync(command);
                return Results.Created($"/api/products/{result.Data}", result);
            });

            return group;
        }
    }
}
```
