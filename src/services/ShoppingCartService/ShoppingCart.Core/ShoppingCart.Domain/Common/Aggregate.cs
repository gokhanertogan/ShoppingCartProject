namespace ShoppingCart.Domain.Common;

public interface IAggregate : IEntity
{
    /// <summary>The aggregate id as a Guid, used as the event stream id.</summary>
    Guid AggregateId { get; }

    /// <summary>
    /// Number of domain events the aggregate has raised over its lifetime. It is the version of
    /// the aggregate's event stream and the optimistic concurrency token of the aggregate row.
    /// </summary>
    long Version { get; }

    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    IDomainEvent[] ClearDomainEvents();
}

public interface IAggregate<T> : IAggregate, IEntity<T>
{

}

public abstract class Aggregate<TId> : Entity<TId>, IAggregate<TId>
    where TId : IStronglyTypedId
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Guid AggregateId => Id.Value;

    public long Version { get; private set; }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
        Version++;
    }

    public IDomainEvent[] ClearDomainEvents()
    {
        IDomainEvent[] dequeueEvents = [.. _domainEvents];
        _domainEvents.Clear();
        return dequeueEvents;
    }
}
