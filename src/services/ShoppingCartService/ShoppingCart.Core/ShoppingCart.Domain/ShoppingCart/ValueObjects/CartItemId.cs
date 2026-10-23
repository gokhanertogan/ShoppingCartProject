using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Domain.ShoppingCart.ValueObjects;

public record CartItemId : IStronglyTypedId
{
    public Guid Value { get; }
    private CartItemId(Guid value) => Value = value;

    public static CartItemId Of(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("CartItemId cannot be empty");
        }

        return new CartItemId(value);
    }

    public override string ToString() => Value.ToString();
}
