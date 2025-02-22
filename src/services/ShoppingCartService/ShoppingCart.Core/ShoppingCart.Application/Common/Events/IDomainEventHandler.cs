using ShoppingCart.Domain.Common;

namespace ShoppingCart.Application.Common.Events;

public interface IDomainEventHandler<T> where T : IDomainEvent
{
    Task HandleAsync(T domainEvent);
}
