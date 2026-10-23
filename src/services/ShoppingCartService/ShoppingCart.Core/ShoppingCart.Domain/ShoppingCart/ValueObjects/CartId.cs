using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Domain.ShoppingCart.ValueObjects;

public record CartId : IStronglyTypedId
{
    public Guid Value { get; }
    private CartId(Guid value) => Value = value;

    public static CartId Of(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("CartId cannot be empty");
        }

        return new CartId(value);
    }

    public override string ToString() => Value.ToString();
}
