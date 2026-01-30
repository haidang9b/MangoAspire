namespace ShoppingCart.API.Dtos;

public class CartDetailsDto
{
    public Guid Id { get; set; }

    public Guid CartHeaderId { get; set; }

    public Guid ProductId { get; set; }

    public int Count { get; set; }
}
