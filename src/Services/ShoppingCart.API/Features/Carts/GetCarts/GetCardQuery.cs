using Mango.Core.Domain;

namespace ShoppingCart.API.Features.Carts.GetCarts;

public record GetCardQuery(string UserId) : IQuery<GetCardResponse>;
