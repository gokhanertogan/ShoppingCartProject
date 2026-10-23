using ShoppingCart.Domain.Common;

namespace ShoppingCart.Application.Common.Persistence;

/// <summary>Append-only log of the domain events of each aggregate, one stream per aggregate.</summary>
public interface IEventStore
{
    /// <summary>
    /// Stages <paramref name="events"/> for appending to the stream of <paramref name="aggregateId"/>.
    /// The events are numbered from <paramref name="expectedVersion"/> + 1 and written when the unit
    /// of work commits; if another writer appended to the stream first, the commit fails with a
    /// concurrency conflict.
    /// </summary>
    Task SaveEventsAsync(Guid aggregateId, string aggregateType, long expectedVersion, IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default);

    /// <summary>Reads the stream of <paramref name="aggregateId"/> in order.</summary>
    Task<IReadOnlyList<RecordedEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}

public sealed record RecordedEvent(Guid AggregateId, string AggregateType, long Version, IDomainEvent Event, DateTime RecordedAt);
