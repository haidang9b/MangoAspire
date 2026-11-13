using Mango.Core.Auth;
using Products.API.Dtos;
using Products.API.Features.Products;

namespace Products.API.Routes;

public static class ProductEndpoints
{
    extension(WebApplication routeGroupBuilder)
    {
        public RouteGroupBuilder MapProductsApi()
        {
            // Admin-only by default, applied to the group so a route added later cannot silently
            // ship unauthenticated. Catalog reads opt out explicitly: Mango.Web's home page and the
            // SPA browse products before anyone signs in.
            var group = routeGroupBuilder.MapGroup("/api/products")
                .WithTags("Products")
                .RequireAuthorization(AuthPolicies.AdminOnly);

            group.MapGet("/", async (int pageIndex, [AsParameters] ProductSearchRequestDto options, ISender sender) =>
            {
                var result = await sender.SendAsync(new GetProducts.Query
                {
                    Options = options
                });
                return Results.Ok(result);
            })
            .AllowAnonymous();


            group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
            {
                var result = await sender.SendAsync(new GetProductById.Query { ProductId = id });
                return Results.Ok(result);
            })
            .AllowAnonymous();

            group.MapPost("/", async (CreateProduct.Command command, ISender sender) =>
            {
                var result = await sender.SendAsync(command);
                return Results.Created($"/api/products/{result.Data}", result);
            });

            group.MapPut("/", async (UpdateProduct.Command command, ISender sender) =>
            {
                var result = await sender.SendAsync(command);
                return Results.Ok(result);
            });

            group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
            {
                var result = await sender.SendAsync(new DeleteProduct.Command { Id = id });
                return Results.Ok(result);
            });

            return group;
        }
    }
}


