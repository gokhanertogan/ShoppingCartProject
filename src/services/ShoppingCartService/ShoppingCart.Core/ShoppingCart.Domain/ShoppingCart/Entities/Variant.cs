using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Entities;

public class Variant : Entity<VariantId>
{
    private Variant() { }
    public VariantId VariantId { get; private set; } = default!;
    public decimal Price { get; private set; } = default!;
}
