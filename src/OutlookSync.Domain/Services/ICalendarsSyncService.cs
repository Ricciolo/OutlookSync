namespace OutlookSync.Domain.Services;

/// <summary>
/// Domain service for synchronizing multiple calendars
/// </summary>
public interface ICalendarsSyncService
{
    /// <summary>
    /// Synchronizes all enabled calendars by copying events between them
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Overall sync result with statistics</returns>
    Task<CalendarsSyncResult> SyncAllCalendarsAsync(CancellationToken cancellationToken = default);

}

/// <summary>
/// Result of synchronizing multiple calendars
/// </summary>
public record CalendarsSyncResult
{
    public required int TotalCalendarsProcessed { get; init; }
    
    public required int SuccessfulSyncs { get; init; }
    
    public required int FailedSyncs { get; init; }
    
    public required int TotalEventsCopied { get; init; }
    
    public IReadOnlyList<string> Errors { get; init; } = [];
    
    public bool IsSuccess => FailedSyncs == 0;
    
    public static CalendarsSyncResult Success(int totalCalendars, int eventsCopied) => 
        new() 
        { 
            TotalCalendarsProcessed = totalCalendars,
            SuccessfulSyncs = totalCalendars,
            FailedSyncs = 0,
            TotalEventsCopied = eventsCopied
        };
    
    public static CalendarsSyncResult Partial(int totalCalendars, int successful, int failed, int eventsCopied, IReadOnlyList<string> errors) => 
        new() 
        { 
            TotalCalendarsProcessed = totalCalendars,
            SuccessfulSyncs = successful,
            FailedSyncs = failed,
            TotalEventsCopied = eventsCopied,
            Errors = errors
        };
}
