using Mango.Core.Domain;
using ShoppingCart.API.Dtos;

namespace ShoppingCart.API.Features.Carts.GetCarts;

public record GetCardQuery(string UserId) : IQuery<GetCardResponse>;
