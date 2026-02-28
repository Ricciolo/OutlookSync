using Microsoft.Extensions.Logging;

namespace OutlookSync.Infrastructure.Repositories;

public partial class ExchangeCalendarEventRepository
{
    [LoggerMessage(LogLevel.Debug, "ExchangeCalendarEventRepository created")]
    private static partial void LogRepositoryCreated(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Repository already initialized")]
    private static partial void LogAlreadyInitialized(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Initializing repository and authenticating")]
    private static partial void LogInitializing(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Token cache updated and saved to credential")]
    private static partial void LogTokenCacheUpdated(ILogger logger);

    [LoggerMessage(LogLevel.Error, "No cached accounts found")]
    private static partial void LogNoCachedAccounts(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Attempting silent token acquisition")]
    private static partial void LogAttemptingSilentAuth(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Token acquired silently")]
    private static partial void LogTokenAcquiredSilently(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Silent token acquisition failed - user interaction required")]
    private static partial void LogSilentAuthFailed(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Repository successfully initialized and authenticated")]
    private static partial void LogInitialized(ILogger logger);

    [LoggerMessage(LogLevel.Error, "MSAL authentication failed")]
    private static partial void LogMsalAuthFailed(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to initialize repository")]
    private static partial void LogFailedToInitialize(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Getting all events for calendar {CalendarExternalId}")]
    private static partial void LogGettingAllEvents(ILogger logger, string calendarExternalId);

    [LoggerMessage(LogLevel.Information, "Found {Count} appointments")]
    private static partial void LogFoundAppointments(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "Getting available calendars for authenticated user")]
    private static partial void LogGettingAvailableCalendars(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Found {Count} available calendars")]
    private static partial void LogFoundCalendars(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "Getting calendar with external ID {CalendarExternalId}")]
    private static partial void LogGettingCalendarById(ILogger logger, string calendarExternalId);

    [LoggerMessage(LogLevel.Information, "Found calendar: {CalendarName}")]
    private static partial void LogFoundCalendar(ILogger logger, string calendarName);

    [LoggerMessage(LogLevel.Warning, "Calendar with ID {CalendarExternalId} not found")]
    private static partial void LogCalendarNotFound(ILogger logger, Exception exception, string calendarExternalId);

    [LoggerMessage(LogLevel.Information, "Finding copied event in target calendar {TargetCalendarExternalId} from source calendar binding {SourceCalendarBindingId} with original event ID {OriginalEventId}")]
    private static partial void LogFindingCopiedEvent(ILogger logger, string targetCalendarExternalId, Guid sourceCalendarBindingId, string originalEventId);

    [LoggerMessage(LogLevel.Information, "Found copied event with ID {EventId}")]
    private static partial void LogFoundCopiedEvent(ILogger logger, Guid eventId);

    [LoggerMessage(LogLevel.Information, "No copied event found")]
    private static partial void LogNoCopiedEventFound(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Adding calendar event '{Subject}' to calendar {CalendarExternalId}")]
    private static partial void LogAddingEvent(ILogger logger, string subject, string calendarExternalId);

    [LoggerMessage(LogLevel.Information, "Calendar event created with ID: {EventId}")]
    private static partial void LogEventCreated(ILogger logger, string eventId);

    [LoggerMessage(LogLevel.Information, "Updating calendar event '{Subject}' with ID {EventId}")]
    private static partial void LogUpdatingEvent(ILogger logger, string subject, string eventId);

    [LoggerMessage(LogLevel.Information, "Calendar event updated successfully: {EventId}")]
    private static partial void LogEventUpdated(ILogger logger, string eventId);

    [LoggerMessage(LogLevel.Information, "Getting all copied events in target calendar {TargetCalendarExternalId} from source calendar binding {SourceCalendarBindingId}")]
    private static partial void LogGettingCopiedEvents(ILogger logger, string targetCalendarExternalId, Guid sourceCalendarBindingId);

    [LoggerMessage(LogLevel.Information, "Found {Count} copied events")]
    private static partial void LogFoundCopiedEvents(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "Deleting event {EventExternalId} from calendar {CalendarExternalId}")]
    private static partial void LogDeletingEvent(ILogger logger, string eventExternalId, string calendarExternalId);

    [LoggerMessage(LogLevel.Information, "Event {EventExternalId} deleted successfully")]
    private static partial void LogEventDeleted(ILogger logger, string eventExternalId);

    [LoggerMessage(LogLevel.Warning, "Event {EventExternalId} not found, possibly already deleted")]
    private static partial void LogEventNotFound(ILogger logger, string eventExternalId);

    [LoggerMessage(LogLevel.Error, "Operation failed after initial attempt and {Attempts} retry attempts")]
    private static partial void LogOperationFailed(ILogger logger, Exception exception, int attempts);

    [LoggerMessage(LogLevel.Warning, "Transient error occurred (attempt {Attempt}/{TotalAttempts}). Retrying in {Delay}ms...")]
    private static partial void LogTransientError(ILogger logger, Exception exception, int attempt, int totalAttempts, int delay);

    [LoggerMessage(LogLevel.Error, "Non-retryable error occurred: {Message}")]
    private static partial void LogNonRetryableError(ILogger logger, Exception exception, string message);
}
