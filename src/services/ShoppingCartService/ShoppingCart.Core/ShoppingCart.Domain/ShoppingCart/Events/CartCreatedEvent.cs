using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.Aggregates;

namespace ShoppingCart.Domain.ShoppingCart.Events;

public record CartCreatedEvent(Cart Cart) : IDomainEvent;
