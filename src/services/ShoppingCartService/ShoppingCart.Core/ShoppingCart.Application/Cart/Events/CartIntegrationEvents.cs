using ShoppingCart.Application.Common.Events;

namespace ShoppingCart.Application.Cart.Events;

// Integration events are the public contract of the service: flat, primitive-typed and versionable
// independently of the domain model. EventId and OccurredOn are copied from the domain event.

public record CartCreatedIntegrationEvent(Guid EventId, DateTime OccurredOn, Guid CartId, Guid CartGuid) : IIntegrationEvent;

public record CartItemAddedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid CartId,
    Guid CartItemId,
    Guid VariantId,
    Guid MerchantId,
    decimal Price,
    int Quantity) : IIntegrationEvent;

public record CartItemQuantityChangedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid CartId,
    Guid CartItemId,
    decimal Price,
    int Quantity) : IIntegrationEvent;

public record CartItemRemovedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid CartId,
    Guid CartItemId,
    Guid VariantId,
    Guid MerchantId) : IIntegrationEvent;

public record CartClearedIntegrationEvent(Guid EventId, DateTime OccurredOn, Guid CartId) : IIntegrationEvent;
