using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Domain.Repositories;

/// <summary>
/// Repository interface for CalendarBinding aggregate
/// </summary>
public interface ICalendarBindingRepository : IRepository<CalendarBinding>
{
    /// <summary>
    /// Checks if a calendar binding already exists for the given source and target calendars.
    /// </summary>
    /// <param name="sourceCalendarId">The source calendar identifier.</param>
    /// <param name="targetCalendarId">The target calendar identifier.</param>
    /// <param name="excludeBindingId">Optional binding ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a duplicate binding exists, false otherwise.</returns>
    Task<bool> ExistsAsync(Guid sourceCalendarId, Guid targetCalendarId, Guid? excludeBindingId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all enabled calendar bindings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of enabled calendar bindings.</returns>
    Task<IReadOnlyList<CalendarBinding>> GetEnabledAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all calendar bindings where the specified calendar is the source.
    /// </summary>
    /// <param name="sourceCalendarId">The source calendar identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of calendar bindings.</returns>
    Task<IReadOnlyList<CalendarBinding>> GetBySourceCalendarAsync(Guid sourceCalendarId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all calendar bindings where the specified calendar is the target.
    /// </summary>
    /// <param name="targetCalendarId">The target calendar identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of calendar bindings.</returns>
    Task<IReadOnlyList<CalendarBinding>> GetByTargetCalendarAsync(Guid targetCalendarId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all calendar bindings involving the specified calendar (as source or target).
    /// </summary>
    /// <param name="calendarId">The calendar identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of calendar bindings.</returns>
    Task<IReadOnlyList<CalendarBinding>> GetByCalendarAsync(Guid calendarId, CancellationToken cancellationToken = default);
}
