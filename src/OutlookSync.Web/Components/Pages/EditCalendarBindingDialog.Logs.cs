namespace OutlookSync.Web.Components.Pages;

public partial class EditCalendarBindingDialog
{
    [LoggerMessage(LogLevel.Error, "Failed to load names for binding {BindingId}")]
    private static partial void LogFailedToLoadNames(ILogger logger, Exception exception, Guid bindingId);

    [LoggerMessage(LogLevel.Information, "Calendar binding {BindingId} updated successfully")]
    private static partial void LogBindingUpdated(ILogger logger, Guid bindingId);

    [LoggerMessage(LogLevel.Error, "Failed to update calendar binding {BindingId}")]
    private static partial void LogFailedToUpdateBinding(ILogger logger, Exception exception, Guid bindingId);
}
