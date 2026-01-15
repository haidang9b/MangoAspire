using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
}
