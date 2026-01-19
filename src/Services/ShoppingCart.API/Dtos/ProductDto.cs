namespace ShoppingCart.API.Dtos;

public class ProductDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public decimal Price { get; set; }

    public required string Description { get; set; }

    public required string CategoryName { get; set; }

    public required string ImageUrl { get; set; }
}
