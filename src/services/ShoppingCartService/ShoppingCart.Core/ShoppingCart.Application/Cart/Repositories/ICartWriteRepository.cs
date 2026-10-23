using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Application.Cart.Repositories;

public interface ICartWriteRepository : IWriteRepository<Domain.ShoppingCart.Aggregates.Cart, CartId>
{
    /// <summary>Loads a tracked cart with its items, or null if it does not exist.</summary>
    Task<Domain.ShoppingCart.Aggregates.Cart?> GetByIdAsync(CartId id, CancellationToken cancellationToken);
}
