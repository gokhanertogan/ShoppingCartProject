using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Domain.ShoppingCart.ValueObjects;

public record CartId
{
    public Guid Value { get; set; }
    private CartId(Guid value) => Value = value;

    public static CartId Of(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("CartId cannot be empty");
        }

        return new CartId(value);
    }
}