using Mango.Core.Auth;
using Products.API.Features.CatalogTypes;

namespace Products.API.Routes;

public static class CatalogTypeEndpoints
{
    extension(WebApplication routeGroupBuilder)
    {
        public RouteGroupBuilder MapCatalogTypesApi()
        {
            // Admin-only by default so a write route added later is not anonymous; the catalog
            // filter list is read before sign-in, so the read opts out.
            var group = routeGroupBuilder.MapGroup("/api/catalog-types")
                .WithTags("Catalog Types")
                .RequireAuthorization(AuthPolicies.AdminOnly);

            group.MapGet("/", async (ISender sender) =>
            {
                var result = await sender.SendAsync(new GetCatalogTypes.Query());
                return Results.Ok(result);
            })
            .AllowAnonymous();

            return group;
        }
    }
}
