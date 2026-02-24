using FluentValidation;
using Mango.Core.Auth;
using Mango.Infrastructure.Auth;
using Mango.Infrastructure.ExceptionHandlers;
using Mango.Infrastructure.Middlewares;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Reflection;

namespace Mango.Infrastructure.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresDbContext<TDbContext>(
        this IServiceCollection services,
        string connectionString,
        Action<IServiceProvider, DbContextOptionsBuilder>? doMoreDbContextOptionsConfigure = null,
        Action<IServiceCollection>? doMoreActions = null
    ) where TDbContext : DbContext, IDbFacadeResolver
    {
        services.AddDbContext<TDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                    connectionString ?? throw new InvalidOperationException(),
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(TDbContext).Assembly.GetName().Name);
                        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }
                ).UseSnakeCaseNamingConvention();

            doMoreDbContextOptionsConfigure?.Invoke(sp, options);
        });

        services.AddScoped<IDbFacadeResolver>(sp => sp.GetRequiredService<TDbContext>());

        doMoreActions?.Invoke(services);

        return services;
    }

    public static IServiceCollection AddValidatorsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        foreach (Type item in from t in assembly.GetTypes()
                              where t.IsClass
                                  && !t.IsAbstract
                                  && t.BaseType != null
                                  && t.BaseType.IsGenericType
                                  && t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>)
                              select t)
        {
            Type type = item.BaseType!.GetGenericArguments()[0];
            Type serviceType = typeof(IValidator<>).MakeGenericType(type);
            services.AddScoped(serviceType, item);
        }

        return services;
    }

    public static IServiceCollection AddDocumentApi(this IServiceCollection services, string title, string version, string description)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(title, new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description
            });
        });

        return services;
    }

    public static IServiceCollection AddCurrentUserContext(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        services.AddTransient<CurrentUserContextMiddleware>();

        return services;
    }

    public static IServiceCollection AddGlobalExceptionHandler(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}
