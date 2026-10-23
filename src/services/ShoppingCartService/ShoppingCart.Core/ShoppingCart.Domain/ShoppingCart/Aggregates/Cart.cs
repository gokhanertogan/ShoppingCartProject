using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.Exceptions;
using ShoppingCart.Domain.ShoppingCart.Entities;
using ShoppingCart.Domain.ShoppingCart.Events;
using ShoppingCart.Domain.ShoppingCart.Exceptions;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Aggregates;

public class Cart : Aggregate<CartId>
{
    /// <summary>Upper bound for the quantity of a single cart line.</summary>
    public const int MaxItemQuantity = 1000;

    public CartGuid CartGuid { get; private set; } = default!;
    private readonly List<CartItem> _items = [];
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public decimal TotalPrice => _items.Sum(item => item.GetTotalPrice());

    public static Cart Create(CartGuid cartGuid)
    {
        var cartId = CartId.Of(Guid.CreateVersion7());
        var cart = new Cart
        {
            Id = cartId,
            CartGuid = cartGuid,
            CreatedAt = DateTime.UtcNow,
        };

        cart.AddDomainEvent(new CartCreatedEvent(cartId, cartGuid));
        return cart;
    }

    /// <summary>
    /// Adds a line to the cart. Adding a variant that is already in the cart from the same merchant
    /// increases the quantity of the existing line instead of creating a new one.
    /// </summary>
    public CartItem AddItem(Variant variant, Merchant merchant, int quantity)
    {
        ValidateQuantity(quantity);

        var item = _items.FirstOrDefault(x => x.Variant.Id == variant.Id && x.Merchant.Id == merchant.Id);
        if (item != null)
        {
            ValidateQuantity(item.Quantity + quantity);
            item.IncreaseQuantity(quantity);
            this.AddDomainEvent(new CartItemUpdatedEvent(this.Id,
                                                       item.Id,
                                                       item.Variant.Price,
                                                       item.Quantity));
            return item;
        }

        var cartItemId = CartItemId.Of(Guid.CreateVersion7());
        item = new CartItem(variant, merchant, quantity) { Id = cartItemId, CreatedAt = DateTime.UtcNow };
        _items.Add(item);
        this.AddDomainEvent(new CartItemAddedEvent(this.Id,
                                                   cartItemId,
                                                   variant.Id,
                                                   merchant.Id,
                                                   variant.Price,
                                                   quantity));
        return item;
    }

    public void UpdateItemQuantity(CartItemId cartItemId, int quantity)
    {
        ValidateQuantity(quantity);

        var item = GetItem(cartItemId);
        if (item.Quantity == quantity)
        {
            return;
        }

        item.SetQuantity(quantity);
        this.AddDomainEvent(new CartItemUpdatedEvent(this.Id,
                                                   item.Id,
                                                   item.Variant.Price,
                                                   item.Quantity));
    }

    public void RemoveItem(CartItemId cartItemId)
    {
        var item = GetItem(cartItemId);
        _items.Remove(item);
        this.AddDomainEvent(new CartItemRemovedEvent(this.Id,
                                                   item.Id,
                                                   item.Variant.Id,
                                                   item.Merchant.Id));
    }

    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        var removedItemCount = _items.Count;
        _items.Clear();
        this.AddDomainEvent(new CartClearedEvent(this.Id, removedItemCount));
    }

    #region  Private Methods
    private CartItem GetItem(CartItemId cartItemId)
    {
        return _items.FirstOrDefault(x => x.Id == cartItemId)
            ?? throw new NotFoundException($"Cart item '{cartItemId.Value}' was not found in cart '{Id.Value}'.");
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new CartException("quantity should be greater than zero");
        }

        if (quantity > MaxItemQuantity)
        {
            throw new CartException($"quantity of a cart line cannot exceed {MaxItemQuantity}");
        }
    }
    #endregion
}
