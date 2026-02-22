using Products.API.Features.Products;

namespace Products.API.Routes;

public static class ProductEndpoints
{
    extension(WebApplication routeGroupBuilder)
    {
        public RouteGroupBuilder MapProductsApi()
        {
            var group = routeGroupBuilder.MapGroup("/api/products")
                .WithTags("Products");

            group.MapGet("/", async (int pageIndex, int pageSize, int? catalogTypeId, ISender sender) =>
            {
                var result = await sender.SendAsync(new GetProducts.Query
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    CatalogTypeId = catalogTypeId
                });
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


