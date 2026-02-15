namespace OutlookSync.Domain.Services;

/// <summary>
/// Result of a synchronization operation
/// </summary>
public record SyncResult
{
    /// <summary>
    /// Gets a value indicating whether the synchronization was successful.
    /// </summary>
    public required bool IsSuccess { get; init; }
    
    /// <summary>
    /// Gets the number of items synchronized.
    /// </summary>
    public required int ItemsSynced { get; init; }
    
    /// <summary>
    /// Gets the error message if the synchronization failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Creates a successful synchronization result.
    /// </summary>
    /// <param name="itemsSynced">The number of items synchronized.</param>
    /// <returns>A successful synchronization result.</returns>
    public static SyncResult Success(int itemsSynced) => 
        new() { IsSuccess = true, ItemsSynced = itemsSynced };
    
    /// <summary>
    /// Creates a failed synchronization result.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed synchronization result.</returns>
    public static SyncResult Failure(string errorMessage) => 
        new() { IsSuccess = false, ItemsSynced = 0, ErrorMessage = errorMessage };
}
