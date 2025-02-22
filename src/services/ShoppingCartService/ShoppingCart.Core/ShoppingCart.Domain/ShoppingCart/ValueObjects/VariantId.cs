using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Domain.ShoppingCart.ValueObjects;

public class VariantId
{
    public Guid Value { get; set; }
    private VariantId(Guid value) => Value = value;

    public static VariantId Of(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("VariantId cannot be empty");
        }
        return new VariantId(value);
    }
}