# Program.cs and service wiring

Moved from the former API-project-structure convention doc and corrected against `src/Services/Products.API` (in-house mediator in `src/Shared/Mediator`, not MediatR).

## Ultra-Clean Program.cs
```csharp
using Products.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();
```


## Dependency Injection & Middleware Configuration

To keep `Program.cs` clean and consistent across all services, use extension methods to group service registrations and pipeline configuration.

### Extension Patterns

#### 1. `WebApplicationBuilderExtensions.cs`
Used for high-level builder configurations like Service Defaults, Data Sources, and service groups.
```csharp
public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.AddNpgsqlDataSource(connectionName: "mydb");
        builder.Services.AddServices(builder.Configuration);
        builder.EnrichNpgsqlDbContext<MyDbContext>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        return builder;
    }
}
```

#### 2. `IServiceCollectionExtensions.cs`
Used for specific `IServiceCollection` registrations like the mediator, pipeline behaviors, validators, DbContext, and Exception Handlers.
```csharp
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();

        services.AddPostgresDbContext<MyDbContext>(
            configuration.GetConnectionString("mydb") 
                ?? throw new ArgumentNullException("mydb"),
            doMoreDbContextOptionsConfigure: (sp, options) =>
            {
                options.AddInterceptors(sp.GetRequiredService<PerformanceInterceptor>());
            });

        services.AddScoped<PerformanceInterceptor>();

        services.AddMediator(typeof(Program).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdentifiedBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TxBehavior<,>));

        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddDocumentApi("My API", "v1", "My API Description");

        return services;
    }
}
```

#### 3. `WebApplicationExtensions.cs`
Used for configuring the request pipeline and asynchronous initialization tasks.
```csharp
public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();
        app.MapDefaultEndpoints();
        app.MapMyApi();

        return app;
    }

    public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        await dbContext.Database.MigrateAsync();
        return app;
    }
}
```
