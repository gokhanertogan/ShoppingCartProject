namespace ShoppingCart.Application.Common.Events;

/// <summary>
/// An event published to other services through <see cref="Messaging.IMessageBus"/>.
/// <see cref="EventId"/> is the id of the domain event it was created from, so redeliveries
/// carry the same id and consumers can de-duplicate.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
