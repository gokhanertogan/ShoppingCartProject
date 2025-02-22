using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.Entities;
using ShoppingCart.Domain.ShoppingCart.Events;
using ShoppingCart.Domain.ShoppingCart.Exceptions;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Aggregates;

public class Cart : Aggregate<CartId>
{
    public CustomerId CustomerId { get; private set; } = default!;
    private readonly List<CartItem> _items = default!;
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public static Cart Create(CustomerId customerId)
    {
        var cartId = CartId.Of(Guid.NewGuid());
        var cart = new Cart
        {
            Id = cartId,
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow,
        };

        cart.AddDomainEvent(new CartCreatedEvent(cart));
        return cart;
    }

    public void AddItem(Variant variant, Merchant merchant, int quantity)
    {
        ValidateQuantity(quantity);

        var item = _items.FirstOrDefault(x => x.Variant == variant && x.Merchant == merchant);
        if (item != null)
        {
            item.IncreaseQuantity(quantity);
        }
        else
        {
            _items.Add(new CartItem(variant, merchant, quantity));
        }
    }

    #region  Private Methods
    private void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new CartException("quantity should be greater than zero");
        }
    }
    #endregion
}