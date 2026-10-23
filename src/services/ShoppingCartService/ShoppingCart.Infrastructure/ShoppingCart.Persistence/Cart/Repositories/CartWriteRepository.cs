using Microsoft.EntityFrameworkCore;
using ShoppingCart.Application.Cart.Repositories;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;
using ShoppingCart.Persistence.Common.Data;
using ShoppingCart.Persistence.Common.Repositories;

namespace ShoppingCart.Persistence.Cart.Repositories;

public class CartWriteRepository(ShoppingCartDbContext context) :
    WriteRepository<Domain.ShoppingCart.Aggregates.Cart, CartId>(context), ICartWriteRepository
{
    public Task<Domain.ShoppingCart.Aggregates.Cart?> GetByIdAsync(CartId id, CancellationToken cancellationToken) =>
        Table
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.Id == id, cancellationToken);
}
