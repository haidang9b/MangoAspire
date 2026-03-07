# API & Endpoints

The API layer acts as a thin wrapper over the MediatR request handlers. Most microservices use a centralized mapping extension to register their endpoints to a WebApplication instance.

## Endpoint Definition
Endpoints are registered using ASP.NET Core Minimal APIs or lightweight API Controllers. The route mapping defines the HTTP verb, path, and security requirements.

### Standard Pattern
1. Validate input parameters and authorize user.
2. Construct the MediatR `Command` or `Query`.
3. Send via `ISender.Send()`.
4. Translate the robust `ResultModel<T>` into a standardized HTTP response.

```csharp
// Example Mapping
app.MapPost("/api/products", async (
    [FromBody] CreateProductDto dto, 
    [FromServices] ISender sender) =>
{
    var command = new CreateProductCommand(dto.Name, dto.Price);
    var result = await sender.Send(command);
    return result.IsSuccess 
        ? Results.Created($"/api/products/{result.Data}", result)
        : Results.BadRequest(result); // Abstracted generally to a ProblemDetails response
});
```

## Security Overview
Endpoints interacting with sensitive resources (e.g., `Orders.API`, `ShoppingCart.API`) explicitly require Authorization headers matching specific policies and scopes provided by the `Identity.API`.

All routing is funneled through the `Mango.Gateway` (YARP). Front-end clients should strictly call the Gateway URLs, avoiding direct internal microservice ports.
