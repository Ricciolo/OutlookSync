using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Services;

/// <summary>
/// Service interface for Exchange Web Services operations
/// </summary>
public interface IExchangeService
{
    /// <summary>
    /// Initializes the Exchange service with authentication token
    /// </summary>
    /// <param name="accessToken">OAuth access token</param>
    /// <param name="serviceUrl">Exchange service URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(string accessToken, string serviceUrl, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets calendar appointments for a specified date range
    /// </summary>
    /// <param name="calendarId">Calendar identifier (folder name or well-known folder)</param>
    /// <param name="startDate">Start date for appointments</param>
    /// <param name="endDate">End date for appointments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of calendar events</returns>
    Task<IReadOnlyList<CalendarEvent>> GetCalendarEventsAsync(
        string calendarId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new calendar appointment
    /// </summary>
    /// <param name="calendarId">Calendar identifier</param>
    /// <param name="calendarEvent">Event to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created event with Exchange ID</returns>
    Task<CalendarEvent> CreateCalendarEventAsync(
        string calendarId,
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing calendar appointment
    /// </summary>
    /// <param name="calendarId">Calendar identifier</param>
    /// <param name="calendarEvent">Event to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateCalendarEventAsync(
        string calendarId,
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a calendar appointment
    /// </summary>
    /// <param name="calendarId">Calendar identifier</param>
    /// <param name="eventExternalId">External event ID from Exchange</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteCalendarEventAsync(
        string calendarId,
        string eventExternalId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests the connection to Exchange service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
