namespace Mango.Web.Models;

public class CouponDto
{
    public Guid Id { get; set; }

    public required string CounponCode { get; set; }

    public decimal DiscountAmount { get; set; }
}
