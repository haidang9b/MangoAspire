namespace Orders.API.Routes;

public static class OrderEndpoints
{
    extension(WebApplication routeGroupBuilder)
    {
        public RouteGroupBuilder MapOrdersApi()
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
}
