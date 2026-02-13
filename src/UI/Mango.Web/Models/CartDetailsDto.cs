namespace Mango.Web.Models;

public class CartDetailsDto
{
    public Guid Id { get; set; }

    public virtual ProductDto? Product { get; set; }

    public int Count { get; set; }
}
