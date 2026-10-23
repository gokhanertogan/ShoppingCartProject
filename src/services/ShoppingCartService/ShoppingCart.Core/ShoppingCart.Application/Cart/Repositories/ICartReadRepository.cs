using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Application.Cart.Repositories;

public interface ICartReadRepository : IReadRepository<Domain.ShoppingCart.Aggregates.Cart, CartId>
{
    /// <summary>Loads an untracked cart with its items, or null if it does not exist.</summary>
    Task<Domain.ShoppingCart.Aggregates.Cart?> FindWithItemsAsync(CartId id, CancellationToken cancellationToken);
}
