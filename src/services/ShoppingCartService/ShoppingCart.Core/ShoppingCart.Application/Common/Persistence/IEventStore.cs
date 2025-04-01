using ShoppingCart.Domain.Common;

namespace ShoppingCart.Application.Common.Persistence;

public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, List<IDomainEvent> events);
    Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId);
}