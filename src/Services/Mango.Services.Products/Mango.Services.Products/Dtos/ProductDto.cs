namespace Mango.Services.Products.Dtos;

public record ProductDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public decimal Price { get; set; }

    public required string Description { get; set; }

    public required string CategoryName { get; set; }

    public required string ImageUrl { get; set; }
}
