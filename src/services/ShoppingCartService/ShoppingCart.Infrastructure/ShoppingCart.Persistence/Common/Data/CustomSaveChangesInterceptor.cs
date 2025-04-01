using Microsoft.EntityFrameworkCore.Diagnostics;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Domain.Common;

namespace ShoppingCart.Persistence.Common.Data
{
    public class CustomSaveChangesInterceptor(IEventStore eventStore) : SaveChangesInterceptor
    {
        private readonly IEventStore _eventStore = eventStore;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context != null)
            {
                var domainEntities = context.ChangeTracker
                    .Entries<IAggregate<object>>()
                    .Where(e => e.Entity.DomainEvents.Any())
                    .ToList();

                var ids = domainEntities
                    .Select(e => (Guid)e.Entity.Id)
                    .Distinct()
                    .ToList();

                foreach (var id in ids)
                {
                    var domainEventsByAggregateId = domainEntities
                        .Where(x => x.Entity.Id.Equals(id))
                        .SelectMany(e => e.Entity.DomainEvents)
                        .ToList();

                    await _eventStore.SaveEventsAsync(id, domainEventsByAggregateId);
                }
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
