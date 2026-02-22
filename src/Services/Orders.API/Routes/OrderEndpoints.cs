using Mango.Core.Auth;
using Microsoft.AspNetCore.Mvc;
using Orders.API.Features.Orders;

namespace Orders.API.Routes;

public static class OrderEndpoints
{
    extension(WebApplication routeGroupBuilder)
    {
        public RouteGroupBuilder MapOrdersApi()
        {
            var group = routeGroupBuilder.MapGroup("/api/orders")
                .WithTags("Orders");

            group.MapGet("/", async (
                int pageIndex,
                int pageSize,
                [FromServices] ISender sender,
                [FromServices] ICurrentUserContext userContext
            ) =>
            {
                var result = await sender.SendAsync(new GetUserOrders.Query
                {
                    UserId = userContext.UserId ?? string.Empty,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                });
                return Results.Ok(result);
            })
            .WithName("GetUserOrders")
            .RequireAuthorization();


            group.MapGet("/{id:guid}", async (
                Guid id,
                [FromServices] ISender sender,
                [FromServices] ICurrentUserContext userContext
            ) =>
            {
                var result = await sender.SendAsync(new GetOrderById.Query
                {
                    OrderId = id,
                    UserId = userContext.UserId ?? string.Empty
                });
                return Results.Ok(result);
            })
            .WithName("GetOrderById")
            .RequireAuthorization();

            return group;
        }
    }
}
