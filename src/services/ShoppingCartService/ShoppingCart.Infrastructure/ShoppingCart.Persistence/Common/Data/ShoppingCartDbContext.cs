using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.Entities;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;
using ShoppingCart.Persistence.Common.Outbox;

namespace ShoppingCart.Persistence.Common.Data;

public class ShoppingCartDbContext(DbContextOptions<ShoppingCartDbContext> options) : DbContext(options)
{
    public DbSet<Domain.ShoppingCart.Aggregates.Cart> Carts { get; set; } = default!;
    public DbSet<CartItem> CartItems { get; set; } = default!;
    public DbSet<StoredEvent> StoredEvents { get; set; } = default!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Domain.ShoppingCart.Aggregates.Cart>(cart =>
        {
            cart.HasKey(c => c.Id);
            cart.Property(c => c.Id).HasConversion(id => id.Value, value => CartId.Of(value));
            cart.Property(c => c.CartGuid).HasConversion(guid => guid.Value, value => CartGuid.Of(value));
            cart.HasIndex(c => c.CartGuid);
            // Version is bumped for every domain event, so concurrent writers of the same cart conflict.
            cart.Property(c => c.Version).IsConcurrencyToken();
            cart.Ignore(c => c.DomainEvents);
            cart.Ignore(c => c.AggregateId);
            cart.Ignore(c => c.TotalPrice);

            cart.HasMany(c => c.Items).WithOne().HasForeignKey("CartId").IsRequired().OnDelete(DeleteBehavior.Cascade);
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
                variant.Property(v => v.Id).HasConversion(id => id.Value, value => VariantId.Of(value)).HasColumnName("VariantId").IsRequired();
                variant.Property(v => v.Price).HasColumnName("Price").HasPrecision(18, 2);
                variant.Ignore(v => v.VariantId);
                IgnoreAuditColumns(variant);
            });
            item.Navigation(i => i.Variant).IsRequired();

            item.OwnsOne(i => i.Merchant, merchant =>
            {
                merchant.Property(m => m.Id).HasConversion(id => id.Value, value => MerchantId.Of(value)).HasColumnName("MerchantId").IsRequired();
                merchant.Property(m => m.Name).HasColumnName("MerchantName").HasMaxLength(Merchant.MaxNameLength);
                merchant.Ignore(m => m.MerchantId);
                IgnoreAuditColumns(merchant);
            });
            item.Navigation(i => i.Merchant).IsRequired();
        });

        modelBuilder.Entity<StoredEvent>(storedEvent =>
        {
            storedEvent.HasKey(e => e.Id);
            storedEvent.Property(e => e.Id).UseIdentityAlwaysColumn();
            storedEvent.Property(e => e.StreamType).HasMaxLength(200);
            storedEvent.Property(e => e.EventType).HasMaxLength(200);
            storedEvent.Property(e => e.Data).HasColumnType("jsonb");
            // Optimistic concurrency for the stream: two writers cannot both append version N.
            storedEvent.HasIndex(e => new { e.StreamId, e.Version }).IsUnique();
            storedEvent.HasIndex(e => e.EventId).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(message =>
        {
            message.HasKey(m => m.Id);
            message.Property(m => m.Type).HasMaxLength(200);
            message.Property(m => m.Content).HasColumnType("jsonb");
            // Partial index: the processor only ever looks at unprocessed messages.
            message.HasIndex(m => new { m.OccurredOn, m.StreamVersion }).HasFilter("\"ProcessedOn\" IS NULL");
        });
    }

    // The cart line already carries the audit columns; the owned snapshots do not need their own.
    private static void IgnoreAuditColumns<TOwner, TOwned>(OwnedNavigationBuilder<TOwner, TOwned> builder)
        where TOwner : class
        where TOwned : class, IEntity
    {
        builder.Ignore(e => e.CreatedAt);
        builder.Ignore(e => e.CreatedBy);
        builder.Ignore(e => e.LastModified);
        builder.Ignore(e => e.LastModifiedBy);
    }
}
