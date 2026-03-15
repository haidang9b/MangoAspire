namespace OpenIdentity.App.Routes;

public static class ClientEndpoints
{

    public static RouteGroupBuilder MapClientsApi(this RouteGroupBuilder group)
    {
        var clientsApi = group.MapGroup("/clients");

        clientsApi.MapGet("/", async (IOpenIddictApplicationManager manager) =>
        {
            var clients = new List<object>();
            await foreach (var client in manager.ListAsync())
            {
                clients.Add(new
                {
                    ClientId = await manager.GetClientIdAsync(client),
                    DisplayName = await manager.GetDisplayNameAsync(client),
                    Type = await manager.GetClientTypeAsync(client)
                });
            }
            return Results.Ok(clients);
        });

        clientsApi.MapPost("/", async (IOpenIddictApplicationManager manager, [FromBody] OpenIddictApplicationDescriptor descriptor) =>
        {
            var client = await manager.CreateAsync(descriptor);
            return Results.Ok(client);
        });

        clientsApi.MapDelete("/{clientId}", async (IOpenIddictApplicationManager manager, string clientId) =>
        {
            var client = await manager.FindByClientIdAsync(clientId);
            if (client != null)
            {
                await manager.DeleteAsync(client);
                return Results.Ok();
            }
            return Results.NotFound();
        });

        return clientsApi;
    }
}

