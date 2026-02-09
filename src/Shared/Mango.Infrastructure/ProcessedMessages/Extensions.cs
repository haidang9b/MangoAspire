using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mango.Infrastructure.ProcessedMessages;

public static class Extensions
{
    public static IServiceCollection AddProcessedMessages<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
    {
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository<TDbContext>>();

        return services;
    }

    public static void ApplyProcessedMessageConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProcessedMessageConfiguration());
    }
}
