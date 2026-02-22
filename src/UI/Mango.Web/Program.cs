using Mango.Core.Options;
using Mango.Infrastructure.Extensions;
using Mango.ServiceDefaults;
using Mango.Web.Filters;
using Mango.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Refit;
using OpenIdConnectOptions = Mango.Core.Options.OpenIdConnectOptions;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<RefitExceptionFilter>();
});
builder.Services.AddCurrentUserContext();
builder.AddServiceDefaults();

// Configure options
builder.Services.Configure<ServiceUrlsOptions>(
    builder.Configuration.GetSection(ServiceUrlsOptions.SectionName));
builder.Services.Configure<OpenIdConnectOptions>(
    builder.Configuration.GetSection(OpenIdConnectOptions.SectionName));

ConfigurationServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseAuthorization();
app.UseCurrentUserContext();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


void ConfigurationServices(IServiceCollection services, IConfiguration configuration)
{
    // Get options from configuration
    var serviceUrls = configuration.GetSection(ServiceUrlsOptions.SectionName).Get<ServiceUrlsOptions>()
        ?? new ServiceUrlsOptions();
    var oidcOptions = configuration.GetSection(OpenIdConnectOptions.SectionName).Get<OpenIdConnectOptions>()
        ?? new OpenIdConnectOptions();

    services.AddRefitClient<IProductsApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.ProductsApi))
        .AddAuthToken();

    services.AddRefitClient<ICartApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.ShoppingCartApi))
        .AddAuthToken();

    services.AddRefitClient<ICouponsApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.CouponsApi))
        .AddAuthToken();

    services.AddRefitClient<IOrdersApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.OrdersApi))
        .AddAuthToken();
    services.AddHttpClient<ChatService>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.ChatAgentApp))
        .AddAuthToken();


    services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies", c => c.ExpireTimeSpan = TimeSpan.FromMinutes(oidcOptions.CookieExpireMinutes))
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = oidcOptions.Authority;
        options.GetClaimsFromUserInfoEndpoint = oidcOptions.GetClaimsFromUserInfoEndpoint;
        options.ClientId = oidcOptions.ClientId;
        options.ClientSecret = oidcOptions.ClientSecret;
        options.ResponseType = oidcOptions.ResponseType;
        options.ClaimActions.MapJsonKey("role", "role", "role");
        options.ClaimActions.MapJsonKey("sub", "sub", "sub");
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
        foreach (var scope in oidcOptions.Scopes)
        {
            options.Scope.Add(scope);
        }
        options.SaveTokens = oidcOptions.SaveTokens;
    });
}
