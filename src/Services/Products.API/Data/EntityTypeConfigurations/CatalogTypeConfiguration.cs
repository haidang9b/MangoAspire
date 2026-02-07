using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Products.API.Data.EntityTypeConfigurations;

public class CatalogTypeConfiguration : IEntityTypeConfiguration<CatalogType>
{
    public void Configure(EntityTypeBuilder<CatalogType> builder)
    {
        var seedData = LoadSeedData();
        if (seedData.Count > 0)
        {
            builder.HasData(seedData);
        }
    }

    private static List<CatalogType> LoadSeedData()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "catalogTypes.json");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<CatalogType>>(json) ?? [];
        }
        return [];
    }
}
