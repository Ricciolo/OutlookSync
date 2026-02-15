using OutlookSync.Domain.Common;

namespace OutlookSync.Domain.Repositories;

/// <summary>
/// Generic repository interface
/// </summary>
/// <typeparam name="T">The entity type that must derive from Entity and implement IAggregateRoot.</typeparam>
public interface IRepository<T> where T : Entity, IAggregateRoot
{
    /// <summary>
    /// Gets a queryable for advanced queries
    /// </summary>
    IQueryable<T> Query { get; }
    
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}
