namespace ShoppingCart.Application.Common.Persistence;

/// <summary>
/// Commits all changes of the current request in one database transaction: the aggregate state,
/// its events in the event store and the outbox messages that publish them.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
