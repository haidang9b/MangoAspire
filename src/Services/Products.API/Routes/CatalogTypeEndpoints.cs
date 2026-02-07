using Products.API.Features.CatalogTypes;

namespace Products.API.Routes;

public static class CatalogTypeEndpoints
{
    extension(WebApplication routeGroupBuilder)
    {
        public RouteGroupBuilder MapCatalogTypesApi()
        {
            var group = routeGroupBuilder.MapGroup("/api/catalog-types")
                .WithTags("Catalog Types");

            group.MapGet("/", async (ISender sender) =>
            {
                var result = await sender.Send(new GetCatalogTypes.Query());
                return Results.Ok(result);
            });

            return group;
        }
    }
}
