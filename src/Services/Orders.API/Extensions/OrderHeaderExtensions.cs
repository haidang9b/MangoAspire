using Orders.API.Entities;

namespace Orders.API.Extensions;

public static class OrderHeaderExtensions
{
    extension(OrderHeader orderHeader)
    {
        public string FullName => $"{orderHeader.FirstName} {orderHeader.LastName}";
    }
}
