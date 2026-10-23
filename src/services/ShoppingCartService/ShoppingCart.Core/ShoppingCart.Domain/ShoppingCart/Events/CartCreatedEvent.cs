using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Events;

public record CartCreatedEvent(CartId CartId, CartGuid CartGuid) : DomainEvent;
