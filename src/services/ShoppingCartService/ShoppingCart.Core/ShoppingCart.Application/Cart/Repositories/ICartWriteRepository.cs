using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Application.Cart.Repositories;

public interface ICartWriteRepository : IWriteRepository<Domain.ShoppingCart.Aggregates.Cart, CartId>
{

}