namespace ShoppingCart.Domain.Common;

public interface IAggregate : IEntity
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    IDomainEvent[] ClearDomainEvents();
}

public interface IAggregate<T> : IAggregate, IEntity<T>
{

}

public abstract class Aggregate<TId> : Entity<TId>, IAggregate<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public string StreamName { get; private set; } = default!;
    public void SetStreamName(string streamName)
        => StreamName = streamName;
    protected bool CheckStreamName()
        => string.IsNullOrEmpty(StreamName) || string.IsNullOrWhiteSpace(StreamName);

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public IDomainEvent[] ClearDomainEvents()
    {
        IDomainEvent[] dequeueEvents = [.. _domainEvents];
        _domainEvents.Clear();
        return dequeueEvents;
    }
}