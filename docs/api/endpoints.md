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

### Modular Routing (OpenIdentity.App Pattern)
For services with multiple distinct API groups, we use a modular routing pattern where endpoints are separated into static classes (e.g., `ClientEndpoints`, `RoleEndpoints`) and registered via extension methods:

```csharp
app.MapGroup("/api")
   .RequireAuthorization("Admin")
   .MapClientsApi()
   .MapResourcesApi();
```

## Security Overview
Endpoints interacting with sensitive resources (e.g., `Orders.API`, `ShoppingCart.API`) explicitly require Authorization headers matching specific policies and scopes provided by the active identity provider.

The JWT `Authority` is never hardcoded. Each service reads it from `ServiceUrls:IdentityApp`, which `Mango.AppHost` sets to the endpoint of whichever provider the `IdentityType` switch selected — see the [Architecture Overview](../architecture/overview.md#identity-provider-switch).

### Provider-Agnostic Scope Policies
The two providers emit the `scope` claim differently: Duende issues one claim per scope, while OpenIddict issues a single space-delimited claim. Policies must accept both, so `ApiScope` splits on whitespace rather than matching a claim value exactly:

```csharp
options.AddPolicy("ApiScope", policy =>
{
    policy.RequireAuthenticatedUser();
    // Duende emits one claim per scope; OpenIddict emits a single
    // space-delimited scope claim. Accept both formats.
    policy.RequireAssertion(context => context.User.FindAll("scope")
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Contains("mango"));
});
```

Use this pattern for any new scope-based policy. A plain `policy.RequireClaim("scope", "mango")` silently passes under Duende and fails under OpenIddict.

All routing is funneled through the `Mango.Gateway` (YARP). Front-end clients should strictly call the Gateway URLs, avoiding direct internal microservice ports.
