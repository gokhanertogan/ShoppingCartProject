using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Persistence.Common.Outbox;

namespace ShoppingCart.Persistence.Common.Data;

/// <summary>
/// Commits the request in one transaction. Domain events are not dispatched in-process here:
/// <see cref="CustomSaveChangesInterceptor"/> writes them to the event store and the outbox as part
/// of the same commit, and <see cref="OutboxProcessor"/> dispatches them afterwards.
/// </summary>
public class UnitOfWork(ShoppingCartDbContext context, OutboxSignal outboxSignal) : IUnitOfWork
{
    private const string StreamVersionIndex = "IX_StoredEvents_StreamId_Version";

    private readonly ShoppingCartDbContext _context = context;
    private readonly OutboxSignal _outboxSignal = outboxSignal;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result;
        try
        {
            result = await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // The aggregate row's Version no longer matches the version that was loaded.
            throw new ConcurrencyConflictException("The aggregate was modified by another request.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: StreamVersionIndex,
        })
        {
            // Another writer appended the same version to the event stream first.
            throw new ConcurrencyConflictException("The aggregate was modified by another request.", exception);
        }

        _outboxSignal.Notify();
        return result;
    }
}
