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
    /// Finds a copied event in the target calendar by its original event external ID and source calendar binding ID
    /// </summary>
    /// <param name="originalEventExternalId">The external ID of the original event</param>
    /// <param name="sourceCalendarBindingId">The Guid of the source calendar binding</param>
    /// <param name="targetCalendarExternalId">The external ID of the target calendar where to search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The copied event if found, otherwise null</returns>
    Task<CalendarEvent?> FindCopiedEventAsync(
        string originalEventExternalId, 
        Guid sourceCalendarBindingId,
        string targetCalendarExternalId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new calendar event to the specified calendar
    /// </summary>
    /// <param name="calendarEvent">The event to add</param>
    /// <param name="calendarExternalId">The external ID of the calendar to add the event to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(CalendarEvent calendarEvent, string calendarExternalId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing calendar event
    /// </summary>
    /// <param name="calendarEvent">The event with updated data</param>
    /// <param name="calendarExternalId">The external ID of the calendar containing the event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(CalendarEvent calendarEvent, string calendarExternalId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all events that were copied from a specific source calendar binding
    /// </summary>
    /// <param name="sourceCalendarBindingId">The Guid of the source calendar binding</param>
    /// <param name="targetCalendarExternalId">The external ID of the target calendar where to search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of copied events</returns>
    Task<IReadOnlyList<CalendarEvent>> GetCopiedEventsAsync(
        Guid sourceCalendarBindingId,
        string targetCalendarExternalId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a calendar event
    /// </summary>
    /// <param name="eventExternalId">The external ID of the event to delete</param>
    /// <param name="calendarExternalId">The external ID of the calendar containing the event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the event was deleted, false if not found</returns>
    Task<bool> DeleteAsync(string eventExternalId, string calendarExternalId, CancellationToken cancellationToken = default);
}
