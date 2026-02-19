using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Common;
using OutlookSync.Domain.Repositories;
using OutlookSync.Infrastructure.Persistence;

namespace OutlookSync.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation with IQueryable support for direct EF Core access
/// </summary>
public class Repository<T>(OutlookSyncDbContext context) : IRepository<T> where T : Entity, IAggregateRoot
{
    protected readonly OutlookSyncDbContext _context = context;

    /// <inheritdoc />
    public virtual IQueryable<T> Query => _context.Set<T>();

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Set<T>().FindAsync([id], cancellationToken);

    /// <inheritdoc />
    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await _context.Set<T>().AddAsync(entity, cancellationToken);

    /// <inheritdoc />
    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Entry(entity).State = EntityState.Deleted;
        return Task.CompletedTask;
    }
}
