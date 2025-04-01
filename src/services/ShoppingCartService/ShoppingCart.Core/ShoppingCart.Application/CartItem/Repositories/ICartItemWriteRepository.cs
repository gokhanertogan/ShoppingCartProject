using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Application.CartItem.Repositories;

public interface ICartItemWriteRepository : IWriteRepository<Domain.ShoppingCart.Entities.CartItem, CartItemId>
{
    
}

