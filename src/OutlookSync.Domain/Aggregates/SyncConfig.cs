using OutlookSync.Domain.Common;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Aggregates;

/// <summary>
/// SyncConfig aggregate - configuration for calendar synchronization
/// </summary>
public class SyncConfig : Entity, IAggregateRoot
{
    public required Guid CalendarId { get; init; }
    
    private SyncConfiguration _configuration = null!;
    
    public required SyncConfiguration Configuration
    {
        get => _configuration;
        init => _configuration = value;
    }
    
    public bool IsEnabled { get; private set; } = true;
    
    public DateTime? LastSyncAt { get; private set; }
    
    public string? LastSyncStatus { get; private set; }
    
    public void UpdateConfiguration(SyncConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        
        _configuration = configuration;
        MarkAsUpdated();
    }
    
    public void Enable()
    {
        IsEnabled = true;
        MarkAsUpdated();
    }
    
    public void Disable()
    {
        IsEnabled = false;
        MarkAsUpdated();
    }
    
    public void RecordSyncSuccess()
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncStatus = "Success";
        MarkAsUpdated();
    }
    
    public void RecordSyncFailure(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        
        LastSyncAt = DateTime.UtcNow;
        LastSyncStatus = $"Failed: {reason}";
        MarkAsUpdated();
    }
}
