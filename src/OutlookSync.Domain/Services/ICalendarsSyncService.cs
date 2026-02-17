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
    /// <summary>
    /// Gets the total number of calendars processed during synchronization.
    /// </summary>
    public required int TotalCalendarsProcessed { get; init; }
    
    /// <summary>
    /// Gets the number of successful synchronizations.
    /// </summary>
    public required int SuccessfulSyncs { get; init; }
    
    /// <summary>
    /// Gets the number of failed synchronizations.
    /// </summary>
    public required int FailedSyncs { get; init; }
    
    /// <summary>
    /// Gets the total number of events copied during synchronization.
    /// </summary>
    public required int TotalEventsCopied { get; init; }
    
    /// <summary>
    /// Gets the collection of error messages from failed synchronizations.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
    
    /// <summary>
    /// Gets a value indicating whether all synchronizations were successful.
    /// </summary>
    public bool IsSuccess => FailedSyncs == 0;
    
    /// <summary>
    /// Creates a successful synchronization result.
    /// </summary>
    /// <param name="totalCalendars">The total number of calendars processed.</param>
    /// <param name="eventsCopied">The total number of events copied.</param>
    /// <returns>A successful synchronization result.</returns>
    public static CalendarsSyncResult Success(int totalCalendars, int eventsCopied) => 
        new() 
        { 
            TotalCalendarsProcessed = totalCalendars,
            SuccessfulSyncs = totalCalendars,
            FailedSyncs = 0,
            TotalEventsCopied = eventsCopied
        };
    
    /// <summary>
    /// Creates a partial synchronization result with both successes and failures.
    /// </summary>
    /// <param name="totalCalendars">The total number of calendars processed.</param>
    /// <param name="successful">The number of successful synchronizations.</param>
    /// <param name="failed">The number of failed synchronizations.</param>
    /// <param name="eventsCopied">The total number of events copied.</param>
    /// <param name="errors">The collection of error messages from failures.</param>
    /// <returns>A partial synchronization result.</returns>
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
