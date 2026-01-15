using MediatR;
using Products.API.Features.Products;

namespace Products.API.Routes;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductsApi(this WebApplication routeGroupBuilder)
    {
        var group = routeGroupBuilder.MapGroup("/api/products")
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

        group.MapPut("/", async (UpdateProduct.Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteProduct.Command { Id = id });
            return Results.Ok(result);
        });

        return group;
    }
}
