using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Domain.Repositories;

/// <summary>
/// Factory for creating calendar event repositories bound to specific calendar and credentials
/// </summary>
public interface ICalendarEventRepositoryFactory
{
    /// <summary>
    /// Creates a repository instance bound to a specific credential and calendar external ID
    /// </summary>
    /// <param name="credential">The credential to use for operations</param>
    /// <param name="calendarExternalId">The external ID of the calendar to operate on (optional - required only for event operations)</param>
    /// <param name="calendarName">Optional friendly name for logging purposes</param>
    /// <returns>Repository instance for the calendar</returns>
    /// <exception cref="InvalidOperationException">Thrown when credential is null, token is invalid, or access token is missing</exception>
    ICalendarEventRepository Create(Credential credential, string? calendarExternalId, string? calendarName = null);
}
