using OutlookSync.Domain.Common;

namespace OutlookSync.Domain.Repositories;

/// <summary>
/// Generic repository interface
/// </summary>
public interface IRepository<T> where T : Entity, IAggregateRoot
{
    /// <summary>
    /// Gets a queryable for advanced queries
    /// </summary>
    IQueryable<T> Query { get; }
    
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}
