using Microsoft.EntityFrameworkCore;
using ShoppingCart.Application.Cart.Repositories;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;
using ShoppingCart.Persistence.Common.Data;
using ShoppingCart.Persistence.Common.Repositories;

namespace ShoppingCart.Persistence.Cart.Repositories;

public class CartReadRepository(ShoppingCartDbContext context) :
    ReadRepository<Domain.ShoppingCart.Aggregates.Cart, CartId>(context), ICartReadRepository
{
    public Task<Domain.ShoppingCart.Aggregates.Cart?> FindWithItemsAsync(CartId id, CancellationToken cancellationToken) =>
        Table
            .AsNoTracking()
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.Id == id, cancellationToken);
}
