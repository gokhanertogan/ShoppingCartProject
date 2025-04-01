using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;
using ShoppingCart.Domain.Validations;

namespace ShoppingCart.Domain.ShoppingCart.Entities;

public class Variant : Entity<VariantId>
{
    private Variant() { }
    public VariantId VariantId { get; private set; } = default!;
    public decimal Price { get; private set; } = default!;
    public Variant(VariantId variantId, decimal price)
    {
        DomainValidation.ValidateMin(nameof(Price), price, 0);
        
        VariantId = variantId;
        Price = price;
        CreatedAt = DateTime.UtcNow;
    }
}
