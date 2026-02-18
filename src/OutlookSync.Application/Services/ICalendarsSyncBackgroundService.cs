namespace OutlookSync.Application.Services;

/// <summary>
/// Interface for the background service that manages calendar synchronization
/// </summary>
public interface ICalendarsSyncBackgroundService
{
    /// <summary>
    /// Gets a value indicating whether a synchronization is currently in progress
    /// </summary>
    bool IsSyncing { get; }
    
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
