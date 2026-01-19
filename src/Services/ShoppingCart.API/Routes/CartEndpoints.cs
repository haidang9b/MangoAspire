using MediatR;
using ShoppingCart.API.Features.Carts;

namespace ShoppingCart.API.Routes;

public static class CartEndpoints
{
    public static RouteGroupBuilder MapCartApi(this WebApplication routeGroupBuilder)
    {
        var group = routeGroupBuilder.MapGroup("/api/carts")
            .WithTags("Carts");

        group.MapGet("/{userId}", async (string userId, ISender sender) =>
        {
            var result = await sender.Send(new GetCart.Query { UserId = userId });
            return Results.Ok(result);
        });

        group.MapPost("/", async (UpsertCart.Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapPut("/", async (UpsertCart.Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapDelete("/item/{cartDetailsId:guid}", async (Guid cartDetailsId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveFromCart.Command { CartDetailsId = cartDetailsId });
            return Results.Ok(result);
        });

        group.MapPost("/coupon", async (ApplyCoupon.Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapDelete("/coupon/{userId}", async (string userId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveCoupon.Command { UserId = userId });
            return Results.Ok(result);
        });

        group.MapPost("/checkout", async (Checkout.Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        return group;
    }
}
