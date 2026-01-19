using Mango.Web.Services;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthenticationDelegatingHandler>();

ConfigurationServices(builder.Services);

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
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


void ConfigurationServices(IServiceCollection services)
{
    services.AddRefitClient<IProductsApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https+http://products-api"))
        .AddHttpMessageHandler<AuthenticationDelegatingHandler>();

    services.AddRefitClient<ICartApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https+http://shoppingcart-api"))
        .AddHttpMessageHandler<AuthenticationDelegatingHandler>();

    services.AddRefitClient<ICouponsApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https+http://coupons-api"))
        .AddHttpMessageHandler<AuthenticationDelegatingHandler>();

    services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies", c => c.ExpireTimeSpan = TimeSpan.FromMinutes(10))
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://identity-app";
        options.GetClaimsFromUserInfoEndpoint = true;
        options.ClientId = "mango";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        //options.ClaimActions.MapJsonKey("role", "role", "role");
        //options.ClaimActions.MapJsonKey("sub", "sub", "sub");
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
        options.Scope.Add("mango");
        options.SaveTokens = true;
    });
}
