using MediatR;
using ShoppingCart.API.Features.Carts;

namespace ShoppingCart.API.Routes;

public static class CartEndpoints
{
    public static RouteGroupBuilder MapCartApi(this WebApplication routeGroupBuilder)
    {
        var group = routeGroupBuilder.MapGroup("/api/cart")
            .WithTags("Cart");

        group.MapGet("/GetCart/{userId}", async (string userId, ISender sender) =>
        {
            var result = await sender.Send(new GetCart.Query { UserId = userId });
            return Results.Ok(result);
        });

        group.MapPost("/AddCart", async (UpsertCart.Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapPost("/UpdateCart", async (UpsertCart.Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapDelete("/RemoveCart/{cartDetailsId:guid}", async (Guid cartDetailsId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveFromCart.Command { CartDetailsId = cartDetailsId });
            return Results.Ok(result);
        });

        group.MapPost("/ApplyCoupon", async (ApplyCoupon.Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapPost("/RemoveCoupon", async (RemoveCoupon.Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapPost("/Checkout", async (Checkout.Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        return group;
    }
}
