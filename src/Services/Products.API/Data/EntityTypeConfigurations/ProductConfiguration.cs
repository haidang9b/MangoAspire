using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Products.API.Data.EntityTypeConfigurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        var seedData = LoadSeedData();
        if (seedData.Count > 0)
        {
            builder.HasData(seedData);
        }
    }

    private static List<Product> LoadSeedData()
    {
        var assembly = typeof(ProductConfiguration).Assembly;
        var resourceName = "Products.API.Data.SeedData.products.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // Fallback to file system for development
            var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "products.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<Product>>(json) ?? [];
            }
            return [];
        }

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return JsonSerializer.Deserialize<List<Product>>(content) ?? [];
    }
}
