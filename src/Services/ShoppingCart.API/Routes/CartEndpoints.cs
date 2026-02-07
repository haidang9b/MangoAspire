using Mango.RestApis.Requests;
using ShoppingCart.API.Features.Carts;
using ShoppingCart.API.Features.Carts.Checkout.Checkout;
using ShoppingCart.API.Features.Carts.GetCarts;

namespace ShoppingCart.API.Routes;

public static class CartEndpoints
{
    extension(WebApplication routeGroupBuilder)
    {
        public RouteGroupBuilder MapCartApi()
        {
            var group = routeGroupBuilder.MapGroup("/api/carts")
                .WithTags("Carts");

            group.MapGet("/{userId}", async (string userId, ISender sender) =>
            {
                var result = await sender.Send(new GetCardQuery(UserId: userId));
                return Results.Ok(result);
            });

            group.MapPost("/", async (AddToCartRequestDto addToCardDto, ISender sender) =>
            {
                var result = await sender.Send(new UpsertCart.Command
                {
                    Cart = addToCardDto
                });
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

            group.MapDelete("/coupon", async (ISender sender) =>
            {
                var result = await sender.Send(new RemoveCoupon.Command());
                return Results.Ok(result);
            });

            group.MapPost("/checkout", async (CheckoutRequestDto command, ISender sender) =>
            {
                var result = await sender.Send(new CheckoutDto
                {
                    CheckoutRequestDto = command
                });
                return Results.Ok(result);
            });

            return group;
        }
    }
}
