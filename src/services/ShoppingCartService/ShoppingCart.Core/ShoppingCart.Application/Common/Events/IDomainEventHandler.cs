using ShoppingCart.Domain.Common;

namespace ShoppingCart.Application.Common.Events;

/// <summary>
/// Handles a domain event after the transaction that raised it has committed. Handlers are invoked
/// by the outbox processor with at-least-once semantics, so they must be idempotent
/// (<see cref="IDomainEvent.EventId"/> is stable and can be used for de-duplication).
/// </summary>
public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    Task HandleAsync(T domainEvent, CancellationToken cancellationToken = default);
}
