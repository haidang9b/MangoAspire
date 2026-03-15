namespace OpenIdentity.App.Routes;

public static class RoleEndpoints
{
    public static RouteGroupBuilder MapRolesApi(this RouteGroupBuilder group)
    {
        var rolesApi = group.MapGroup("/roles");

        rolesApi.MapGet("/", (RoleManager<IdentityRole> manager) => Results.Ok(manager.Roles.ToList()));

        rolesApi.MapPost("/", async (RoleManager<IdentityRole> manager, string name) =>
        {
            if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest();
            var result = await manager.CreateAsync(new IdentityRole(name));
            return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
        });

        rolesApi.MapDelete("/{name}", async (RoleManager<IdentityRole> manager, string name) =>
        {
            var role = await manager.FindByNameAsync(name);
            if (role != null)
            {
                await manager.DeleteAsync(role);
                return Results.Ok();
            }
            return Results.NotFound();
        });

        return rolesApi;
    }
}

