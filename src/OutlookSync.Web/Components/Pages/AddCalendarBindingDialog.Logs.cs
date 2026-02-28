namespace OutlookSync.Web.Components.Pages;

public partial class AddCalendarBindingDialog
{
    [LoggerMessage(LogLevel.Error, "Failed to load credentials")]
    private static partial void LogFailedToLoadCredentials(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to load source calendars")]
    private static partial void LogFailedToLoadSourceCalendars(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to load target calendars")]
    private static partial void LogFailedToLoadTargetCalendars(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Calendar binding {BindingName} added successfully with ID {BindingId}")]
    private static partial void LogBindingAdded(ILogger logger, string bindingName, Guid bindingId);

    [LoggerMessage(LogLevel.Error, "Failed to save calendar binding")]
    private static partial void LogFailedToSaveBinding(ILogger logger, Exception exception);
}
