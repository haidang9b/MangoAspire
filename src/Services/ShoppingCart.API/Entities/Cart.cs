namespace ShoppingCart.API.Entities;

public class Cart
{
    public required CartHeader CartHeader { get; set; }

    public IEnumerable<CartDetails> CartDetails { get; set; } = [];
}
