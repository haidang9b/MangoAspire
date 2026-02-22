using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.API.Initializer;
using Identity.API.Models;
using Identity.API.Services;

namespace Identity.API.Extensions;

public static class IdentityServerExtensions
{
    public static IServiceCollection AddIdentityServerConfiguration(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDBInitializer, DBInitializer>();
        services.AddScoped<IProfileService, ProfileService>();

        var identityResources = configuration.GetSection("IdentityServer:IdentityResources").Get<List<IdentityResource>>() ?? new();
        var apiScopes = configuration.GetSection("IdentityServer:ApiScopes").Get<List<ApiScope>>() ?? new();
        var clients = configuration.GetSection("IdentityServer:Clients").Get<List<Client>>() ?? new();

        foreach (var client in clients)
        {
            foreach (var secret in client.ClientSecrets)
            {
                secret.Value = secret.Value.ToSha256();
            }
        }

        services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            options.EmitStaticAudienceClaim = true;
        })
        .AddInMemoryIdentityResources(identityResources)
        .AddInMemoryApiScopes(apiScopes)
        .AddInMemoryClients(clients)
        .AddAspNetIdentity<ApplicationUser>()
        .AddProfileService<ProfileService>()
        .AddDeveloperSigningCredential();

        return services;
    }
}
