using Mango.Core.Domain;

namespace Products.API.Entities;

public class Product : EntityBase<Guid>
{
    public required string Name { get; set; }

    public decimal Price { get; set; }

    public required string Description { get; set; }

    public required string CategoryName { get; set; }

    public required string ImageUrl { get; set; }

    public int AvailableStock { get; set; }

    public int? CatalogTypeId { get; set; }

    public int? CatalogBrandId { get; set; }

    public virtual CatalogType? CatalogType { get; set; }

    public virtual CatalogBrand? CatalogBrand { get; set; }
}
