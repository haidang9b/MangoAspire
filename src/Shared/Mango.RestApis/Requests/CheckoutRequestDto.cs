namespace Mango.RestApis.Requests;

public class CheckoutRequestDto
{
    public string? CouponCode { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal OrderTotal { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateTime PickupDate { get; set; }

    public string? Phone { get; set; }

    public required string Email { get; set; }

    public required string CardNumber { get; set; }

    public required string CVV { get; set; }

    public required string ExpiryMonthYear { get; set; }

    public int CartTotalItems { get; set; }
}
