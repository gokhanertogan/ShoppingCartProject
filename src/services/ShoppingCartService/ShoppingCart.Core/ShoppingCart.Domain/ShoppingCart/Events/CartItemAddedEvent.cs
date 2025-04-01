using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Events;

public record CartItemAddedEvent(
    CartId CartId,
    CartItemId CartItemId,
    VariantId VariantId,
    MerchantId MerchantId,
    decimal Price,
    int Quantity) : IDomainEvent;
