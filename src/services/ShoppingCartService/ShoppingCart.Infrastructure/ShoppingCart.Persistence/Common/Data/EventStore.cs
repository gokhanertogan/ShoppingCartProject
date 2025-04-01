using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Domain.Common;
using EventStore.ClientAPI;
using System.Text.Json;
using System.Text;

namespace ShoppingCart.Persistence.Common.Data;

public class EventStore(IEventStoreConnection client) : IEventStore
{
    private readonly IEventStoreConnection _client = client;

    public async Task SaveEventsAsync(Guid aggregateId, List<IDomainEvent> events)
    {
        var expectedVersion = await GetExpectedVersionForAggregate(aggregateId);
        var eventStreamName = GetStreamName(aggregateId);

        var eventData = events.Select(e =>
        {
            var jsonData = JsonSerializer.Serialize(e);
            var metadata = GetMetadata(e);

            return new EventData(
                Guid.NewGuid(),
                e.GetType().Name,
                true,
                Encoding.UTF8.GetBytes(jsonData),
                metadata != null ? Encoding.UTF8.GetBytes(metadata) : null
            );
        }).ToArray();

        var writeResult = await _client.AppendToStreamAsync(
            eventStreamName,
            expectedVersion,
            eventData
        );
    }

    public async Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId)
    {
        var eventStreamName = GetStreamName(aggregateId);
        var events = new List<IDomainEvent>();

        var result = _client.ReadStreamEventsForwardAsync(eventStreamName, StreamPosition.Start, 4096, false, null);
        var streamEventsSlice = await result;
        foreach (var resolvedEvent in streamEventsSlice.Events)
        {
            var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());
            var domainEvent = JsonSerializer.Deserialize<IDomainEvent>(eventData)!;
            events.Add(domainEvent);
        }
        
        return events;
    }

    private static string GetStreamName(Guid aggregateId)
    {
        return $"aggregate-{aggregateId}";
    }

    private static string GetMetadata(IDomainEvent domainEvent)
    {
        return $"Event type: {domainEvent.GetType().Name}, OccurredOn: {domainEvent.OccurredOn}";
    }

    private async Task<long> GetExpectedVersionForAggregate(Guid aggregateId)
    {
        var eventStreamName = GetStreamName(aggregateId);
        var result = await _client.ReadStreamEventsBackwardAsync(eventStreamName, StreamPosition.End, 1, false, null);
        long streamPosition = StreamPosition.Start;

        if (result.Events.Length != 0)
        {
            streamPosition = result.Events[0].Event.EventNumber;
        }

        return streamPosition == StreamPosition.Start ? -1 : streamPosition;
    }
}