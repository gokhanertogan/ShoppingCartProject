using Microsoft.EntityFrameworkCore;
using ShoppingCart.Domain.ShoppingCart.Entities;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Persistence.Common.Data;

public class ShoppingCartDbContext(DbContextOptions<ShoppingCartDbContext> options) : DbContext(options)
{
    public DbSet<Domain.ShoppingCart.Aggregates.Cart> Carts { get; set; } = default!;
    public DbSet<CartItem> CartItems { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Domain.ShoppingCart.Aggregates.Cart>(cart =>
        {
            cart.HasKey(c => c.Id);
            cart.Property(c => c.Id).HasConversion(id => id.Value, value => CartId.Of(value));
            cart.Property(c => c.CartGuid).HasConversion(guid => guid.Value, value => CartGuid.Of(value));
            cart.HasIndex(c => c.CartGuid);
            cart.Ignore(c => c.DomainEvents);
            cart.Ignore(c => c.StreamName);

            cart.HasMany(c => c.Items).WithOne().HasForeignKey("CartId").OnDelete(DeleteBehavior.Cascade);
            cart.Navigation(c => c.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<CartItem>(item =>
        {
            item.HasKey(i => i.Id);
            item.Property(i => i.Id).HasConversion(id => id.Value, value => CartItemId.Of(value));

            // Variant and Merchant are stored as a snapshot on the cart line (price and merchant name
            // at the time the item was added), so they are mapped as owned types rather than shared tables.
            item.OwnsOne(i => i.Variant, variant =>
            {
                variant.Property(v => v.Id).HasConversion(id => id.Value, value => VariantId.Of(value)).HasColumnName("VariantId");
                variant.Property(v => v.Price).HasColumnName("Price").HasPrecision(18, 2);
                variant.Ignore(v => v.VariantId);
            });

            item.OwnsOne(i => i.Merchant, merchant =>
            {
                merchant.Property(m => m.Id).HasConversion(id => id.Value, value => MerchantId.Of(value)).HasColumnName("MerchantId");
                merchant.Property(m => m.Name).HasColumnName("MerchantName");
                merchant.Ignore(m => m.MerchantId);
            });
        });
    }
}
