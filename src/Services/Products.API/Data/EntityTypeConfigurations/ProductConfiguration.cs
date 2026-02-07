using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Products.API.Data.EntityTypeConfigurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasData(
            new Product
            {
                Id = Guid.Parse("970afb7e-5616-46c4-b3fa-f9abb62928db"),
                Name = "Samosa",
                Price = 15m,
                Description =
                    "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. " +
                    "Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>" +
                    "Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, " +
                    "maximus malesuada neque. Phasellus commodo cursus pretium.",
                ImageUrl = "https://phanhaidang.blob.core.windows.net/mango/14.jpg",
                CategoryName = "Appetizer"
            },
            new Product
            {
                Id = Guid.Parse("348cc1a0-707b-4e1b-9700-de40d54acd31"),
                Name = "Paneer Tikka",
                Price = 13.99m,
                Description =
                    "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. " +
                    "Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>" +
                    "Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, " +
                    "maximus malesuada neque. Phasellus commodo cursus pretium.",
                ImageUrl = "https://phanhaidang.blob.core.windows.net/mango/12.jpg",
                CategoryName = "Appetizer"
            },
            new Product
            {
                Id = Guid.Parse("bc047267-5eb9-4681-b489-4473ccd00a87"),
                Name = "Sweet Pie",
                Price = 10.99m,
                Description =
                    "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. " +
                    "Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>" +
                    "Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, " +
                    "maximus malesuada neque. Phasellus commodo cursus pretium.",
                ImageUrl = "https://phanhaidang.blob.core.windows.net/mango/11.jpg",
                CategoryName = "Dessert"
            },
            new Product
            {
                Id = Guid.Parse("7e51b0af-8c5f-4721-99bb-4aaf8fc4822e"),
                Name = "Pav Bhaji",
                Price = 15m,
                Description =
                    "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. " +
                    "Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>" +
                    "Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, " +
                    "maximus malesuada neque. Phasellus commodo cursus pretium.",
                ImageUrl = "https://phanhaidang.blob.core.windows.net/mango/13.jpg",
                CategoryName = "Entree"
            }
        );
    }
}
