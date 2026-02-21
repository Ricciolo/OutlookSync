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
    private readonly OutlookSyncDbContext _context = context;

    /// <summary>
    /// Gets the database context for use by derived classes.
    /// </summary>
    protected OutlookSyncDbContext Context => _context;

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
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Unchanged)
        {
            // When an owned-entity reference has been replaced (e.g., via a record 'with' expression),
            // the old owned-entity instance remains in EF Core's identity map even after the parent is
            // detached. Clearing the tracker removes all stale entries so that _context.Update can
            // re-read the current values (including the new owned-entity object) without conflicts.
            _context.ChangeTracker.Clear();
        }

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
