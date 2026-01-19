namespace Orders.API.Routes;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrdersApi(this WebApplication routeGroupBuilder)
    {
        var group = routeGroupBuilder.MapGroup("/api/orders")
            .WithTags("Orders");

        // Add additional routes here as needed
        // For example:
        // group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        // {
        //     var result = await sender.Send(new GetOrderById.Query { OrderId = id });
        //     return Results.Ok(result);
        // });

        return group;
    }
}
