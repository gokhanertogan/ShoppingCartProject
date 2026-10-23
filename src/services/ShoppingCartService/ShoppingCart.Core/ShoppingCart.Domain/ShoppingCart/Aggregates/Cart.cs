using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.Entities;
using ShoppingCart.Domain.ShoppingCart.Events;
using ShoppingCart.Domain.ShoppingCart.Exceptions;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Aggregates;

public class Cart : Aggregate<CartId>
{
    public CartGuid CartGuid { get; private set; } = default!;
    private readonly List<CartItem> _items = [];
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public static Cart Create(CartGuid cartGuid)
    {
        var cartId = CartId.Of(Guid.NewGuid());
        var cart = new Cart
        {
            Id = cartId,
            CartGuid = cartGuid,
            CreatedAt = DateTime.UtcNow,
        };

        cart.AddDomainEvent(new CartCreatedEvent(cart));
        return cart;
    }

    public void AddItem(Variant variant, Merchant merchant, int quantity)
    {
        ValidateQuantity(quantity);

        var item = _items.FirstOrDefault(x => x.Variant.Id == variant.Id && x.Merchant.Id == merchant.Id);
        if (item != null)
        {
            item.IncreaseQuantity(quantity);
            this.AddDomainEvent(new CartItemUpdatedEvent(this.Id,
                                                       item.Id,
                                                       variant.Price,
                                                       item.Quantity));
        }
        else
        {
            var cartItemId = CartItemId.Of(Guid.NewGuid());
            _items.Add(new CartItem(variant, merchant, quantity) { Id = cartItemId });
            this.AddDomainEvent(new CartItemAddedEvent(this.Id,
                                                       cartItemId,
                                                       variant.Id,
                                                       merchant.Id,
                                                       variant.Price,
                                                       quantity));
        }
    }

    #region  Private Methods
    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new CartException("quantity should be greater than zero");
        }
    }
    #endregion
}
