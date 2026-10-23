using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShoppingCart.Domain.Common;
using ShoppingCart.Persistence.Common.Outbox;
using ShoppingCart.Persistence.Common.Serialization;

namespace ShoppingCart.Persistence.Common.Data
{
    /// <summary>
    /// Runs inside SaveChanges, before the transaction is committed:
    /// <list type="bullet">
    /// <item>sets the audit columns (CreatedAt / LastModified),</item>
    /// <item>appends the pending domain events of every tracked aggregate to its event stream,</item>
    /// <item>writes the same events to the outbox, to be dispatched after commit.</item>
    /// </list>
    /// All of it is part of the same database transaction as the aggregate state.
    /// </summary>
    public sealed class CustomSaveChangesInterceptor(EventSerializer serializer) : SaveChangesInterceptor
    {
        private readonly EventSerializer _serializer = serializer;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            CaptureDomainEvents(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            CaptureDomainEvents(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            ClearDomainEvents(eventData.Context);
            return base.SavedChanges(eventData, result);
        }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            ClearDomainEvents(eventData.Context);
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            DiscardStagedRows(eventData.Context);
            base.SaveChangesFailed(eventData);
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            DiscardStagedRows(eventData.Context);
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        private void CaptureDomainEvents(DbContext? context)
        {
            if (context is not ShoppingCartDbContext dbContext)
            {
                return;
            }

            dbContext.ChangeTracker.DetectChanges();
            var now = DateTime.UtcNow;

            foreach (var entry in dbContext.ChangeTracker.Entries<IEntity>())
            {
                if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.LastModified = now;
                }
            }

            var aggregates = dbContext.ChangeTracker
                .Entries<IAggregate>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            var eventStore = new EventStore(dbContext, _serializer);
            foreach (var aggregate in aggregates)
            {
                var domainEvents = aggregate.DomainEvents;

                // Version already counts the pending events; the stream currently ends before them.
                var expectedVersion = aggregate.Version - domainEvents.Count;
                eventStore.Stage(aggregate.AggregateId, aggregate.GetType().Name, expectedVersion, domainEvents, now);

                // EF Core does not insert rows in the order they were added, so the dispatch order is
                // carried by the data: OccurredOn, then the stream version for events of the same instant.
                var streamVersion = expectedVersion;
                foreach (var domainEvent in domainEvents)
                {
                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = domainEvent.EventId,
                        Type = EventSerializer.GetEventType(domainEvent),
                        Content = _serializer.Serialize(domainEvent),
                        OccurredOn = domainEvent.OccurredOn,
                        StreamVersion = ++streamVersion,
                    });
                }
            }
        }

        private static void ClearDomainEvents(DbContext? context)
        {
            if (context is null)
            {
                return;
            }

            foreach (var entry in context.ChangeTracker.Entries<IAggregate>())
            {
                entry.Entity.ClearDomainEvents();
            }
        }

        // If the commit fails, the staged event and outbox rows must not be written by a later retry
        // on the same context: the aggregate still holds its events and stages them again.
        private static void DiscardStagedRows(DbContext? context)
        {
            if (context is null)
            {
                return;
            }

            var staged = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added && e.Entity is StoredEvent or OutboxMessage)
                .ToList();

            foreach (var entry in staged)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
