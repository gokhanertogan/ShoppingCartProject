namespace ShoppingCart.Domain.Common;

public interface IDomainEvent
{
    Guid EventId => Guid.NewGuid();

    public DateTime OccurredOn => DateTime.Now;

    public string EventType => GetType().AssemblyQualifiedName!;
}