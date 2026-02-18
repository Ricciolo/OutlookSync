using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Repositories;

/// <summary>
/// Repository interface for calendar events
/// </summary>
public interface ICalendarEventRepository
{
    /// <summary>
    /// Initializes the repository and performs any necessary setup operations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all available calendars for the authenticated user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available calendars</returns>
    Task<IReadOnlyList<AvailableCalendar>> GetAvailableCalendarsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific calendar by its external ID
    /// </summary>
    /// <param name="calendarExternalId">The calendar external ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The calendar if found, otherwise null</returns>
    Task<AvailableCalendar?> GetAvailableCalendarByIdAsync(string calendarExternalId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all events for the specified calendar
    /// </summary>
    /// <param name="calendarExternalId">The calendar external ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<CalendarEvent>> GetAllAsync(string calendarExternalId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a copied event in the target calendar by its original event external ID and source calendar external ID
    /// </summary>
    /// <param name="originalEventExternalId">The external ID of the original event</param>
    /// <param name="sourceCalendarExternalId">The external ID of the source calendar</param>
    /// <param name="targetCalendarExternalId">The external ID of the target calendar where to search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The copied event if found, otherwise null</returns>
    Task<CalendarEvent?> FindCopiedEventAsync(
        string originalEventExternalId, 
        string sourceCalendarExternalId,
        string targetCalendarExternalId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new calendar event to the specified calendar
    /// </summary>
    /// <param name="calendarEvent">The event to add</param>
    /// <param name="calendarExternalId">The external ID of the calendar to add the event to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(CalendarEvent calendarEvent, string calendarExternalId, CancellationToken cancellationToken = default);
}
