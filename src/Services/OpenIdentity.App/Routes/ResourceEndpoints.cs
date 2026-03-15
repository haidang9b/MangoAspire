namespace OpenIdentity.App.Routes;

public static class ResourceEndpoints
{
    public static RouteGroupBuilder MapResourcesApi(this RouteGroupBuilder group)
    {
        var resourcesApi = group.MapGroup("/resources");

        resourcesApi.MapGet("/", async (IOpenIddictScopeManager manager) =>
        {
            var scopes = new List<object>();
            await foreach (var scope in manager.ListAsync())
            {
                scopes.Add(scope);
            }
            return Results.Ok(scopes);
        });

        resourcesApi.MapPost("/", async (IOpenIddictScopeManager manager, [FromBody] OpenIddictScopeDescriptor descriptor) =>
        {
            var scope = await manager.CreateAsync(descriptor);
            return Results.Ok(scope);
        });

        resourcesApi.MapDelete("/{name}", async (IOpenIddictScopeManager manager, string name) =>
        {
            var scope = await manager.FindByNameAsync(name);
            if (scope != null)
            {
                await manager.DeleteAsync(scope);
                return Results.Ok();
            }
            return Results.NotFound();
        });

        return resourcesApi;
    }
}

