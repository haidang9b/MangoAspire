using Mango.Core.Options;
using Mango.Infrastructure.Extensions;
using Mango.ServiceDefaults;
using OpenIdentity.App.Services;

namespace OpenIdentity.App.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddOpenIdentityServices(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.AddNpgsqlDbContext<ApplicationDbContext>("openidentitydb");

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ??
            ["http://localhost:5173"];
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("spa", policy =>
            {
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            })
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/connect/token")
                       .SetAuthorizationEndpointUris("/connect/authorize")
                       .SetUserInfoEndpointUris("/connect/userinfo")
                       .SetEndSessionEndpointUris("/connect/endsession");

                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange()
                       .AllowClientCredentialsFlow()
                       .AllowRefreshTokenFlow();

                // Downstream APIs validate access tokens with plain AddJwtBearer,
                // which cannot decrypt OpenIddict's default JWE tokens. Emit
                // signed-only (JWS) access tokens, like Duende does.
                options.DisableAccessTokenEncryption();

                options.RegisterScopes("openid", "profile", "email", "roles", "mango", "offline_access");

                // Use ASP.NET Core Data Protection and endpoint generation
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough();

                // Match Identity.API pattern: Use persistent certificates for development for cross-restart token validity.
                if (builder.Environment.IsDevelopment())
                {
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();
                }
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        builder.Services.Configure<SeedUsersOptions>(
            builder.Configuration.GetSection(SeedUsersOptions.SectionName));

        builder.Services.AddScoped<IDbInitializer, DbInitializer>();

        builder.Services.AddCacheManager();
        builder.Services.AddScoped<UserProfileCache>();

        return builder;
    }
}
