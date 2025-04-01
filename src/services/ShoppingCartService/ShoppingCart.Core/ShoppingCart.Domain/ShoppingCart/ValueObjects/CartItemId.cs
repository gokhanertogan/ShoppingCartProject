using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Domain.ShoppingCart.ValueObjects;

public record CartItemId
{
    public Guid Value { get; set; }
    private CartItemId(Guid value) => Value = value;

    public static CartItemId Of(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("CartItemId cannot be empty");
        }

        return new CartItemId(value);
    }
}