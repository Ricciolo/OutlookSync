using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Domain.Services;

/// <summary>
/// Strategy interface for calendar synchronization
/// </summary>
public interface ISyncStrategy
{
    Task<SyncResult> SyncAsync(Calendar calendar, Device device, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a synchronization operation
/// </summary>
public record SyncResult
{
    public required bool IsSuccess { get; init; }
    
    public required int ItemsSynced { get; init; }
    
    public string? ErrorMessage { get; init; }
    
    public static SyncResult Success(int itemsSynced) => 
        new() { IsSuccess = true, ItemsSynced = itemsSynced };
    
    public static SyncResult Failure(string errorMessage) => 
        new() { IsSuccess = false, ItemsSynced = 0, ErrorMessage = errorMessage };
}
