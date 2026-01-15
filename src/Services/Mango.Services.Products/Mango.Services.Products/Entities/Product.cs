using Mango.Core.Domain;

namespace Mango.Services.Products.Entities;

public class Product : EntityBase<Guid>
{
    public required string Name { get; set; }

    public decimal Price { get; set; }

    public required string Description { get; set; }

    public required string CategoryName { get; set; }

    public required string ImageUrl { get; set; }
}
