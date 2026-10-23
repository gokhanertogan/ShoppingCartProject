namespace ShoppingCart.Persistence.Common.Data;

/// <summary>One row of the append-only event log. (StreamId, Version) is unique.</summary>
public sealed class StoredEvent
{
    public long Id { get; private set; }
    public Guid StreamId { get; init; }
    public string StreamType { get; init; } = default!;
    public long Version { get; init; }
    public Guid EventId { get; init; }
    public string EventType { get; init; } = default!;
    public string Data { get; init; } = default!;
    public DateTime OccurredOn { get; init; }
    public DateTime RecordedAt { get; init; }
}
