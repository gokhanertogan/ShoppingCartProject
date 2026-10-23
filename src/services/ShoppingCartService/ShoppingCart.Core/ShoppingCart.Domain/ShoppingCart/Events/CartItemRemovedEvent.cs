using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Events;

public record CartItemRemovedEvent(
    CartId CartId,
    CartItemId CartItemId,
    VariantId VariantId,
    MerchantId MerchantId) : DomainEvent;
