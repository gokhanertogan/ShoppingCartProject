using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Domain.Common;
using ShoppingCart.Persistence.Common.Data;

namespace ShoppingCart.Persistence.Common.Repositories;

public class WriteRepository<T, TId>(ShoppingCartDbContext context) : IWriteRepository<T, TId> where T : class, IEntity<TId>
{
    private readonly ShoppingCartDbContext _context = context;
    public DbSet<T> Table => _context.Set<T>();

    public async Task<bool> AddAsync(T model, CancellationToken cancellationToken)
    {
        EntityEntry<T> entityEntry = await Table.AddAsync(model, cancellationToken);
        return entityEntry.State == EntityState.Added;
    }

    public async Task<bool> AddRangeAsync(List<T> datas, CancellationToken cancellationToken)
    {
        await Table.AddRangeAsync(datas, cancellationToken);
        return true;
    }

    public async Task<bool> RemoveAsync(TId id, CancellationToken cancellationToken)
    {
        var model = await Table.FirstOrDefaultAsync(data => data.Id!.Equals(id), cancellationToken: cancellationToken);
        if (model == null) return false;

        EntityEntry<T> entityEntry = Table.Remove(model);
        return entityEntry.State == EntityState.Deleted;
    }

    public bool RemoveRange(List<T> datas)
    {
        Table.RemoveRange(datas);
        return true;
    }

    public bool Update(T model)
    {
        EntityEntry entityEntry = Table.Update(model);
        return entityEntry.State == EntityState.Modified;
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        

        await _context.SaveChangesAsync(cancellationToken);
    }
}