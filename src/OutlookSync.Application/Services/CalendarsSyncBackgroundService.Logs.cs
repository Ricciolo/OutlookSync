using Microsoft.Extensions.Logging;

namespace OutlookSync.Application.Services;

public partial class CalendarsSyncBackgroundService
{
    [LoggerMessage(LogLevel.Information, "Rescheduling all calendar bindings")]
    private static partial void LogReschedulingAll(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Manual synchronization of all bindings triggered")]
    private static partial void LogManualSyncAllTriggered(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Manual synchronization of binding {BindingId} triggered")]
    private static partial void LogManualSyncBindingTriggered(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Warning, "Cannot trigger sync for binding {BindingId}: already syncing")]
    private static partial void LogAlreadySyncing(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Information, "Calendar synchronization service started")]
    private static partial void LogServiceStarted(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Calendar synchronization service stopping")]
    private static partial void LogServiceStopping(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Loaded {Count} enabled calendar bindings")]
    private static partial void LogBindingsLoaded(ILogger logger, int count);

    [LoggerMessage(LogLevel.Debug, "Binding {BindingId} ({Name}) scheduled for next sync at {NextSyncAt}")]
    private static partial void LogBindingScheduled(ILogger logger, Guid bindingId, string name, DateTime nextSyncAt);

    [LoggerMessage(LogLevel.Error, "Error loading calendar bindings")]
    private static partial void LogErrorLoadingBindings(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Started scheduling loop for binding {BindingId} ({Name})")]
    private static partial void LogSchedulingLoopStarted(ILogger logger, Guid bindingId, string name);

    [LoggerMessage(LogLevel.Debug, "Binding {BindingId} ({Name}) will sync in {Delay}")]
    private static partial void LogBindingWillSync(ILogger logger, Guid bindingId, string name, TimeSpan delay);

    [LoggerMessage(LogLevel.Information, "Binding {BindingId} was disabled, stopping scheduling loop")]
    private static partial void LogBindingDisabled(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Debug, "Scheduling loop cancelled for binding {BindingId}")]
    private static partial void LogSchedulingLoopCancelled(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Error, "Error in scheduling loop for binding {BindingId}")]
    private static partial void LogErrorInSchedulingLoop(ILogger logger, Exception exception, Guid bindingId);

    [LoggerMessage(LogLevel.Debug, "Scheduling loop ended for binding {BindingId}")]
    private static partial void LogSchedulingLoopEnded(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Warning, "Binding {BindingId} not found in states")]
    private static partial void LogBindingNotFound(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Information, "Starting synchronization for binding {BindingId} ({Name})")]
    private static partial void LogSyncStarted(ILogger logger, Guid bindingId, string name);

    [LoggerMessage(LogLevel.Information, "Synchronization completed successfully for binding {BindingId} ({Name}), {EventCount} events synced")]
    private static partial void LogSyncCompleted(ILogger logger, Guid bindingId, string name, int eventCount);

    [LoggerMessage(LogLevel.Warning, "Synchronization failed for binding {BindingId} ({Name}): {Error}")]
    private static partial void LogSyncFailed(ILogger logger, Guid bindingId, string name, string? error);

    [LoggerMessage(LogLevel.Error, "Error during synchronization of binding {BindingId} ({Name})")]
    private static partial void LogSyncError(ILogger logger, Exception exception, Guid bindingId, string name);

    [LoggerMessage(LogLevel.Debug, "Binding {BindingId} ({Name}) next sync scheduled at {NextSyncAt}")]
    private static partial void LogNextSyncScheduled(ILogger logger, Guid bindingId, string name, DateTime nextSyncAt);

    [LoggerMessage(LogLevel.Error, "Error reloading binding {BindingId} after sync")]
    private static partial void LogErrorReloadingBinding(ILogger logger, Exception exception, Guid bindingId);

    [LoggerMessage(LogLevel.Information, "Waiting for {Count} active synchronizations to complete")]
    private static partial void LogWaitingForActiveSyncs(ILogger logger, int count);
}
