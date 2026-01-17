using Duende.IdentityServer.Services;
using Identity.API.Initializer;
using Identity.API.Models;
using Identity.API.Services;

namespace Identity.API.Extensions;

public static class IdentityServerExtensions
{
    public static IServiceCollection AddIdentityServerConfiguration(
        this IServiceCollection services)
    {
        services.AddScoped<IDBInitializer, DBInitializer>();
        services.AddScoped<IProfileService, ProfileService>();

        services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            options.EmitStaticAudienceClaim = true;
        })
        .AddInMemoryIdentityResources(SD.IdentityResources)
        .AddInMemoryApiScopes(SD.ApiScopes)
        .AddInMemoryClients(SD.Clients)
        .AddAspNetIdentity<ApplicationUser>()
        .AddDeveloperSigningCredential();

        return services;
    }
}
