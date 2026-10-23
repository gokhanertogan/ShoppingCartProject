using ShoppingCart.Application.Common.Events;
using ShoppingCart.Application.Common.Messaging;
using ShoppingCart.Domain.ShoppingCart.Events;

namespace ShoppingCart.Application.Cart.Events;

/// <summary>Translates committed cart domain events into integration events on the message bus.</summary>
public class CartIntegrationEventPublisher(IMessageBus messageBus) :
    IDomainEventHandler<CartCreatedEvent>,
    IDomainEventHandler<CartItemAddedEvent>,
    IDomainEventHandler<CartItemUpdatedEvent>,
    IDomainEventHandler<CartItemRemovedEvent>,
    IDomainEventHandler<CartClearedEvent>
{
    private readonly IMessageBus _messageBus = messageBus;

    public Task HandleAsync(CartCreatedEvent domainEvent, CancellationToken cancellationToken = default) =>
        _messageBus.PublishAsync(new CartCreatedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.OccurredOn,
            domainEvent.CartId.Value,
            domainEvent.CartGuid.Value), cancellationToken);

    public Task HandleAsync(CartItemAddedEvent domainEvent, CancellationToken cancellationToken = default) =>
        _messageBus.PublishAsync(new CartItemAddedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.OccurredOn,
            domainEvent.CartId.Value,
            domainEvent.CartItemId.Value,
            domainEvent.VariantId.Value,
            domainEvent.MerchantId.Value,
            domainEvent.Price,
            domainEvent.Quantity), cancellationToken);

    public Task HandleAsync(CartItemUpdatedEvent domainEvent, CancellationToken cancellationToken = default) =>
        _messageBus.PublishAsync(new CartItemQuantityChangedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.OccurredOn,
            domainEvent.CartId.Value,
            domainEvent.CartItemId.Value,
            domainEvent.Price,
            domainEvent.Quantity), cancellationToken);

    public Task HandleAsync(CartItemRemovedEvent domainEvent, CancellationToken cancellationToken = default) =>
        _messageBus.PublishAsync(new CartItemRemovedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.OccurredOn,
            domainEvent.CartId.Value,
            domainEvent.CartItemId.Value,
            domainEvent.VariantId.Value,
            domainEvent.MerchantId.Value), cancellationToken);

    public Task HandleAsync(CartClearedEvent domainEvent, CancellationToken cancellationToken = default) =>
        _messageBus.PublishAsync(new CartClearedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.OccurredOn,
            domainEvent.CartId.Value), cancellationToken);
}
