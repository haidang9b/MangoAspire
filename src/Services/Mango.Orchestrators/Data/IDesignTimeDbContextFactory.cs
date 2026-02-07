using Mango.SagaOrchestrators.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Products.API.Data;

public class ProductDbContextFactory
    : IDesignTimeDbContextFactory<SagaDbContext>
{
    public SagaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SagaDbContext>();

        optionsBuilder.UseNpgsql("productdb")
            .UseSnakeCaseNamingConvention();

        return new SagaDbContext(optionsBuilder.Options);
    }
}
