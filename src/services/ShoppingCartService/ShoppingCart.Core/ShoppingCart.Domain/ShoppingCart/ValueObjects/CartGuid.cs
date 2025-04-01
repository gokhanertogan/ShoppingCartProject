using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Domain.ShoppingCart.ValueObjects;

public record CartGuid
{
    public Guid Value { get; }
    private CartGuid(Guid value) => Value = value;

    public static CartGuid Of(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("CartGuid cannot be empty");
        }

        return new CartGuid(value);
    }
}