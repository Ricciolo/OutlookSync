using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Domain.Repositories;

/// <summary>
/// Repository interface for CalendarBinding aggregate
/// </summary>
public interface ICalendarBindingRepository : IRepository<CalendarBinding>
{
    /// <summary>
    /// Checks if a calendar binding already exists for the given source and target calendar identifiers.
    /// </summary>
    /// <param name="sourceCredentialId">The source credential identifier.</param>
    /// <param name="sourceExternalId">The source calendar external identifier.</param>
    /// <param name="targetCredentialId">The target credential identifier.</param>
    /// <param name="targetExternalId">The target calendar external identifier.</param>
    /// <param name="excludeBindingId">Optional binding ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a duplicate binding exists, false otherwise.</returns>
    Task<bool> ExistsAsync(
        Guid sourceCredentialId, 
        string sourceExternalId, 
        Guid targetCredentialId, 
        string targetExternalId, 
        Guid? excludeBindingId = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all enabled calendar bindings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of enabled calendar bindings.</returns>
    Task<IReadOnlyList<CalendarBinding>> GetEnabledAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all calendar bindings where the specified credential and external ID is the source.
    /// </summary>
    /// <param name="sourceCredentialId">The source credential identifier.</param>
    /// <param name="sourceExternalId">The source calendar external identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of calendar bindings.</returns>
    Task<IReadOnlyList<CalendarBinding>> GetBySourceAsync(
        Guid sourceCredentialId, 
        string sourceExternalId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all calendar bindings where the specified credential and external ID is the target.
    /// </summary>
    /// <param name="targetCredentialId">The target credential identifier.</param>
    /// <param name="targetExternalId">The target calendar external identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of calendar bindings.</returns>
    Task<IReadOnlyList<CalendarBinding>> GetByTargetAsync(
        Guid targetCredentialId, 
        string targetExternalId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all calendar bindings involving the specified credential and external ID (as source or target).
    /// </summary>
    /// <param name="credentialId">The credential identifier.</param>
    /// <param name="externalId">The calendar external identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of calendar bindings.</returns>
    Task<IReadOnlyList<CalendarBinding>> GetByCredentialAndExternalIdAsync(
        Guid credentialId, 
        string externalId, 
        CancellationToken cancellationToken = default);
}
