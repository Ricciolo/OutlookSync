namespace OutlookSync.Application.Services;

/// <summary>
/// Information about a scheduled binding synchronization
/// </summary>
public record ScheduledBindingInfo
{
    /// <summary>
    /// Gets the calendar binding identifier
    /// </summary>
    public required Guid BindingId { get; init; }
    
    /// <summary>
    /// Gets the binding name
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets the last synchronization time
    /// </summary>
    public DateTime? LastSyncAt { get; init; }
    
    /// <summary>
    /// Gets the next scheduled synchronization time
    /// </summary>
    public DateTime NextSyncAt { get; init; }
    
    /// <summary>
    /// Gets whether the binding is currently syncing
    /// </summary>
    public bool IsSyncing { get; init; }
    
    /// <summary>
    /// Gets the synchronization interval in minutes
    /// </summary>
    public int IntervalMinutes { get; init; }
}

/// <summary>
/// Interface for the background service that manages calendar synchronization
/// </summary>
public interface ICalendarsSyncBackgroundService
{
    /// <summary>
    /// Gets a value indicating whether any synchronization is currently in progress
    /// </summary>
    bool IsSyncing { get; }
    
    /// <summary>
    /// Gets a value indicating whether automatic synchronization is enabled
    /// </summary>
    bool IsAutoSyncEnabled { get; }
    
    /// <summary>
    /// Gets the currently scheduled bindings with their next sync times
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of scheduled binding information</returns>
    Task<IReadOnlyList<ScheduledBindingInfo>> GetScheduledBindingsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reschedules all bindings (call this when bindings are added, updated, or deleted)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RescheduleAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Triggers a synchronization of all enabled calendar bindings
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sync was triggered, false if already syncing</returns>
    Task<bool> TriggerSyncAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Triggers a synchronization of a specific calendar binding
    /// </summary>
    /// <param name="bindingId">The ID of the binding to synchronize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sync was triggered, false if already syncing</returns>
    Task<bool> TriggerSyncBindingAsync(Guid bindingId, CancellationToken cancellationToken = default);
}
