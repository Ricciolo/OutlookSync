using OutlookSync.Domain.Common;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Aggregates;

/// <summary>
/// Calendar aggregate - represents an Outlook calendar to sync
/// </summary>
public class Calendar : Entity, IAggregateRoot
{
    public required string Name { get; init; }
    
    public required string ExternalId { get; init; }
    
    public required Guid CredentialId { get; init; }
    
    public bool IsEnabled { get; private set; } = true;
    
    public int SyncDaysForward { get; set; } = 30;
    
    private SyncConfiguration _configuration = null!;
    
    public required SyncConfiguration Configuration
    {
        get => _configuration;
        init => _configuration = value;
    }
    
    public DateTime? LastSyncAt { get; private set; }
    
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
    
    public void UpdateConfiguration(SyncConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        
        _configuration = configuration;
        MarkAsUpdated();
    }
    
    public void RecordSuccessfulSync(int itemsSynced)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemsSynced, nameof(itemsSynced));
        
        LastSyncAt = DateTime.UtcNow;
        
        MarkAsUpdated();
    }
    
    public void RecordFailedSync(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        
        LastSyncAt = DateTime.UtcNow;
        
        MarkAsUpdated();
    }
}
