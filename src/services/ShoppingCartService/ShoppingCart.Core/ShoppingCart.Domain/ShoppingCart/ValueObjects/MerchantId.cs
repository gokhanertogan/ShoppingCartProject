using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Domain.ShoppingCart.ValueObjects;

public record MerchantId
{
    public Guid Value { get; }
    private MerchantId(Guid value) => Value = value;

    public static MerchantId Of(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("MerchantId cannot be empty");
        }

        return new MerchantId(value);
    }
}