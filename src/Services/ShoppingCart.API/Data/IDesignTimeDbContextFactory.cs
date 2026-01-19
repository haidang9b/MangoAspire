using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShoppingCart.API.Data;

public class ProductDbContextFactory
    : IDesignTimeDbContextFactory<ShoppingCartDbContext>
{
    public ShoppingCartDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShoppingCartDbContext>();

        optionsBuilder.UseNpgsql("shoppingcartdb")
            .UseSnakeCaseNamingConvention();

        return new ShoppingCartDbContext(optionsBuilder.Options);
    }
}
