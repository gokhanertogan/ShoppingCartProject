using Microsoft.EntityFrameworkCore;
using ShoppingCart.Domain.ShoppingCart.Entities;

namespace ShoppingCart.Persistence.Common.Data;

public class ShoppingCartDbContext(DbContextOptions<ShoppingCartDbContext> options) : DbContext(options)
{
    public DbSet<Domain.ShoppingCart.Aggregates.Cart> Carts { get; set; } = default!;
    public DbSet<CartItem> CartItems { get; set; } = default!;
    public DbSet<Merchant> Merchants { get; set; } = default!;
    public DbSet<Variant> Variants { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}