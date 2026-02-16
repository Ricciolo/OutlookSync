using OutlookSync.Domain.Aggregates;
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
    /// Gets all events for the calendar this repository was created for
    /// </summary>
    Task<IReadOnlyList<CalendarEvent>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a copied event by its source event and source calendar
    /// </summary>
    Task<CalendarEvent?> FindCopiedEventAsync(CalendarEvent sourceEvent, Calendar sourceCalendar, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new calendar event
    /// </summary>
    Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);
}

