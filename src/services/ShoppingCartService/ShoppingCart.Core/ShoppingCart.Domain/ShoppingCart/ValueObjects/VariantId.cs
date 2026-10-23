using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Domain.ShoppingCart.ValueObjects;

public record VariantId : IStronglyTypedId
{
    public Guid Value { get; }
    private VariantId(Guid value) => Value = value;

    public static VariantId Of(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("VariantId cannot be empty");
        }

        return new VariantId(value);
    }

    public override string ToString() => Value.ToString();
}
