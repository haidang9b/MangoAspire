using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
}
