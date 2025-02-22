using ShoppingCart.Domain.Common;

namespace ShoppingCart.Persistence.Common.Events.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents);
}