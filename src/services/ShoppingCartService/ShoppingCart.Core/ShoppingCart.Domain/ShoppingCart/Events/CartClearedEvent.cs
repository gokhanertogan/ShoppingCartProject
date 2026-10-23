using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Events;

public record CartClearedEvent(CartId CartId, int RemovedItemCount) : DomainEvent;
