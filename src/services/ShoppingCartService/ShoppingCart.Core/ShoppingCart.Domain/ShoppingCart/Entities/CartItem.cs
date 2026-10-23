using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Entities;

public class CartItem(Variant variant, Merchant merchant, int quantity) : Entity<CartItemId>
{
    // Required by EF Core: navigations cannot be bound to constructor parameters.
    private CartItem() : this(default!, default!, default) { }

    public Variant Variant { get; private set; } = variant;
    public Merchant Merchant { get; private set; } = merchant;
    public int Quantity { get; private set; } = quantity;

    // Quantity rules are enforced by the Cart aggregate, which is the only caller.
    internal void IncreaseQuantity(int quantity)
    {
        Quantity += quantity;
    }

    internal void DecreaseQuantity(int quantity)
    {
        Quantity -= quantity;
    }

    internal void SetQuantity(int quantity)
    {
        Quantity = quantity;
    }

    public decimal GetTotalPrice()
    {
        return Quantity * Variant.Price;
    }
}
