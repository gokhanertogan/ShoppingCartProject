using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Domain.Common;
using ShoppingCart.Persistence.Common.Events.Interfaces;

namespace ShoppingCart.Persistence.Common.Data;

public class UnitOfWork(ShoppingCartDbContext context, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    private readonly ShoppingCartDbContext _context = context;
    private readonly IDomainEventDispatcher _dispatcher = dispatcher;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<IAggregate<object>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        var result = await _context.SaveChangesAsync(cancellationToken);

        foreach (var entity in domainEntities) //TODO - check if this is needed
        {
            entity.Entity.ClearDomainEvents();
        }

        await _dispatcher.DispatchAsync(domainEvents);
        return result;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
