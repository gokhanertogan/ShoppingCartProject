using Microsoft.EntityFrameworkCore;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Domain.Common;
using ShoppingCart.Persistence.Common.Serialization;

namespace ShoppingCart.Persistence.Common.Data;

/// <summary>
/// PostgreSQL-backed event store. Events are rows of the <c>StoredEvents</c> table, written in the
/// same transaction as the aggregate state, so the state and its history can never diverge.
/// </summary>
public sealed class EventStore(ShoppingCartDbContext context, EventSerializer serializer) : IEventStore
{
    private readonly ShoppingCartDbContext _context = context;
    private readonly EventSerializer _serializer = serializer;

    public Task SaveEventsAsync(Guid aggregateId, string aggregateType, long expectedVersion, IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        Stage(aggregateId, aggregateType, expectedVersion, events, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    internal void Stage(Guid aggregateId, string aggregateType, long expectedVersion, IReadOnlyList<IDomainEvent> events, DateTime recordedAt)
    {
        var version = expectedVersion;
        foreach (var domainEvent in events)
        {
            _context.StoredEvents.Add(new StoredEvent
            {
                StreamId = aggregateId,
                StreamType = aggregateType,
                Version = ++version,
                EventId = domainEvent.EventId,
                EventType = EventSerializer.GetEventType(domainEvent),
                Data = _serializer.Serialize(domainEvent),
                OccurredOn = domainEvent.OccurredOn,
                RecordedAt = recordedAt,
            });
        }
    }

    public async Task<IReadOnlyList<RecordedEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.StoredEvents
            .AsNoTracking()
            .Where(e => e.StreamId == aggregateId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new RecordedEvent(
            row.StreamId,
            row.StreamType,
            row.Version,
            _serializer.Deserialize(row.EventType, row.Data),
            row.RecordedAt))];
    }
}
