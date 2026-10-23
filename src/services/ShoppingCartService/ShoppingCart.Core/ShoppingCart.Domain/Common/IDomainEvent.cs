namespace ShoppingCart.Domain.Common;

public interface IDomainEvent
{
    /// <summary>Unique id of the event, assigned once when the event is raised.</summary>
    Guid EventId { get; }

    /// <summary>UTC time at which the event was raised.</summary>
    DateTime OccurredOn { get; }

    string EventType => GetType().Name;
}

/// <summary>
/// Base record for domain events. <see cref="EventId"/> and <see cref="OccurredOn"/> are captured
/// once at construction (and restored as-is when an event is deserialized), so they are stable
/// across reads, persistence and redelivery.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
