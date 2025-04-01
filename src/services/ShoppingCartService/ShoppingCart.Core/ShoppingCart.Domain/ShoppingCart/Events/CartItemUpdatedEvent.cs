using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Events;

public record CartItemUpdatedEvent(
    CartId CartId,
    CartItemId CartItemId,
    decimal Price,
    int Quantity) : IDomainEvent;