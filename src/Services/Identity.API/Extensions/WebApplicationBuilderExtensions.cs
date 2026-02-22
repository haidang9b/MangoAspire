using Identity.API.Data;
using Identity.API.Models;
using Mango.ServiceDefaults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddIdentityApiDefaults(
        this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        builder.AddNpgsqlDataSource(connectionName: "identitydb");

        builder.Services.AddControllersWithViews();

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                    builder.Configuration.GetConnectionString("coupondb"))
                .UseSnakeCaseNamingConvention());

        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddIdentityServerConfiguration(builder.Configuration);

        return builder;
    }
}
