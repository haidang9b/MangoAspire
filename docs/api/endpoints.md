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
Every business API validates bearer tokens through one shared extension, `AddMangoJwtAuthentication(configuration)` in `Mango.Infrastructure/Extensions/JwtAuthenticationExtensions.cs`, paired with `UseAuthentication()` / `UseAuthorization()` in the service's `UseApiPipeline()`. Do not configure `AddJwtBearer` or authorization policies per service.

The JWT `Authority` is never hardcoded. The extension reads it from `ServiceUrls:IdentityApp`, which `Mango.AppHost` sets to the endpoint of whichever provider the `IdentityType` switch selected — see the [Architecture Overview](../architecture/overview.md#identity-provider-switch). A service that validates tokens needs `.WithReference(identityRef)` and `ServiceUrls__IdentityApp` in `AppHost.cs`.

### Shared policies
Policy names are constants in `Mango.Core.Auth.AuthPolicies`; role names are in `Mango.Core.Auth.AuthRoles`.

| Policy | Requires |
|---|---|
| `AuthPolicies.ApiScope` | An authenticated caller whose token carries the `mango` scope. |
| `AuthPolicies.AdminOnly` | `ApiScope` plus the `Admin` role. |

| Service | Anonymous | `ApiScope` | `AdminOnly` |
|---|---|---|---|
| Products.API | `GET /api/products`, `GET /api/products/{id}`, `GET /api/catalog-types` (catalog browsing before sign-in) | — | `POST`/`PUT`/`DELETE /api/products`, `POST /api/products/cdc-snapshots` |
| Coupons.API | — | `GET /api/coupons/{code}` | — |
| ShoppingCart.API, ChatAgent.App | — | all business routes | — |
| Orders.API | — | — (any authenticated caller, the default policy; the `mango` scope is not checked yet) | — |

Apply the policy to the **route group** and opt individual reads out with `.AllowAnonymous()`, so a route added to the group later is protected by default. `Products.API.Tests/Routes/RouteAuthorizationTests` fails if any route other than the known catalog reads is anonymous.

`Admin` is granted only through the `SeedUsers` configuration. Self-registration in both identity providers always assigns `Customer`.

### Provider-Agnostic Scope Policies
The two providers emit the `scope` claim differently: Duende issues one claim per scope, while OpenIddict issues a single space-delimited claim. Policies must accept both, so the shared `RequireMangoScope()` policy-builder extension splits on whitespace rather than matching a claim value exactly:

```csharp
public AuthorizationPolicyBuilder RequireMangoScope() => policy
    .RequireAuthenticatedUser()
    .RequireAssertion(context => context.User.FindAll("scope")
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Contains(MangoScope));
```

Build any new scope-based policy on `RequireMangoScope()`. A plain `policy.RequireClaim("scope", "mango")` silently passes under Duende and fails under OpenIddict.

Audiences are not validated yet (`ValidateAudience = false`): neither provider issues a per-API audience, so any Mango access token is accepted by every API.

All routing is funneled through the `Mango.Gateway` (YARP). Front-end clients should strictly call the Gateway URLs, avoiding direct internal microservice ports.
