using System.ComponentModel.DataAnnotations;

namespace Mango.Web.Models;

public class ProductDto
{
    public ProductDto()
    {
        Count = 1;
    }
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    [Display(Name = "Catalog Type")]
    public int? CatalogTypeId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    [Display(Name = "Stock")]
    public int Stock { get; set; }

    [Range(1, 100)]
    public int Count { get; set; }
}

public class CatalogTypeDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
}

