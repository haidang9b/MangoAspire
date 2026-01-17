using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Products.API.Data;

public class ProductDbContextFactory
    : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductDbContext>();

        optionsBuilder.UseNpgsql("productdb")
            .UseSnakeCaseNamingConvention();

        return new ProductDbContext(optionsBuilder.Options);
    }
}
