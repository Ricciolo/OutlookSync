using Microsoft.Extensions.Logging;

namespace OutlookSync.Application.Services;

public partial class CalendarsSyncService
{
    [LoggerMessage(LogLevel.Information, "Starting synchronization using calendar bindings")]
    private static partial void LogStartingSyncAll(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "No enabled calendar bindings found for synchronization")]
    private static partial void LogNoBindingsFound(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Error syncing binding {BindingId}")]
    private static partial void LogErrorSyncingBinding(ILogger logger, Exception exception, Guid bindingId);

    [LoggerMessage(LogLevel.Information, "Synchronization completed. Total: {Total}, Successful: {Successful}, Failed: {Failed}, Events copied: {EventsCopied}")]
    private static partial void LogSyncAllCompleted(ILogger logger, int total, int successful, int failed, int eventsCopied);

    [LoggerMessage(LogLevel.Information, "Starting synchronization for binding {BindingId}")]
    private static partial void LogStartingSyncBinding(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Warning, "Calendar binding {BindingId} not found")]
    private static partial void LogBindingNotFound(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Warning, "Calendar binding {BindingId} is disabled")]
    private static partial void LogBindingDisabled(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Information, "Changes saved successfully for binding {BindingId}")]
    private static partial void LogChangesSaved(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Error, "Failed to save changes after syncing binding {BindingId}")]
    private static partial void LogErrorSavingChanges(ILogger logger, Exception exception, Guid bindingId);

    [LoggerMessage(LogLevel.Information, "Found {TotalEvents} events in source calendar '{SourceName}', {OriginalEvents} are original (not copied)")]
    private static partial void LogEventsFound(ILogger logger, int totalEvents, string sourceName, int originalEvents);

    [LoggerMessage(LogLevel.Information, "After filtering, {EventsToSync} events will be synchronized for binding {BindingName}")]
    private static partial void LogEventsAfterFiltering(ILogger logger, int eventsToSync, string bindingName);

    [LoggerMessage(LogLevel.Debug, "Event {EventSubject} has changed, updating")]
    private static partial void LogEventChanged(ILogger logger, string eventSubject);

    [LoggerMessage(LogLevel.Debug, "Updated event {EventSubject} for binding {BindingName}")]
    private static partial void LogEventUpdated(ILogger logger, string eventSubject, string bindingName);

    [LoggerMessage(LogLevel.Debug, "Event {EventSubject} unchanged, skipping")]
    private static partial void LogEventUnchanged(ILogger logger, string eventSubject);

    [LoggerMessage(LogLevel.Debug, "Copied event {EventSubject} for binding {BindingName}")]
    private static partial void LogEventCopied(ILogger logger, string eventSubject, string bindingName);

    [LoggerMessage(LogLevel.Error, "Error syncing event {EventId} for binding {BindingName}")]
    private static partial void LogErrorSyncingEvent(ILogger logger, Exception exception, string eventId, string bindingName);

    [LoggerMessage(LogLevel.Information, "Found {CopiedEventCount} copied events in target calendar for binding {BindingName}")]
    private static partial void LogCopiedEventsFound(ILogger logger, int copiedEventCount, string bindingName);

    [LoggerMessage(LogLevel.Debug, "Deleting orphaned event {EventSubject} (original ID: {OriginalId}) from target")]
    private static partial void LogDeletingOrphanedEvent(ILogger logger, string eventSubject, string? originalId);

    [LoggerMessage(LogLevel.Debug, "Deleted orphaned event {EventSubject} from target")]
    private static partial void LogOrphanedEventDeleted(ILogger logger, string eventSubject);

    [LoggerMessage(LogLevel.Error, "Error deleting event {EventId} for binding {BindingName}")]
    private static partial void LogErrorDeletingEvent(ILogger logger, Exception exception, string eventId, string bindingName);

    [LoggerMessage(LogLevel.Information, "Successfully synced {EventCount} events for binding {BindingName} ({ModifiedCount} modified)")]
    private static partial void LogSyncSuccess(ILogger logger, int eventCount, string bindingName, int modifiedCount);

    [LoggerMessage(LogLevel.Debug, "Credentials updated for binding {BindingName}")]
    private static partial void LogCredentialsUpdated(ILogger logger, string bindingName);

    [LoggerMessage(LogLevel.Error, "Failed to update credentials for binding {BindingId}")]
    private static partial void LogErrorUpdatingCredentials(ILogger logger, Exception exception, Guid bindingId);
}
