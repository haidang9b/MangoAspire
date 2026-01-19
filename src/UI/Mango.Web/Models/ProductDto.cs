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

    public string ImageUrl { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Count { get; set; }
}
