using OutlookSync.Domain.Common;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Aggregates;

/// <summary>
/// Calendar aggregate - represents an Outlook calendar to sync
/// </summary>
public class Calendar : Entity, IAggregateRoot
{
    /// <summary>
    /// Gets the name of the calendar.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets the external identifier of the calendar from the source system.
    /// </summary>
    public required string ExternalId { get; init; }
    
    /// <summary>
    /// Gets the credential identifier used to access this calendar.
    /// </summary>
    public required Guid CredentialId { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether the calendar is enabled for synchronization.
    /// </summary>
    public bool IsEnabled { get; private set; } = true;
    
    /// <summary>
    /// Gets or sets the number of days forward to synchronize.
    /// </summary>
    public int SyncDaysForward { get; set; } = 30;
    
    private SyncConfiguration _configuration = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Calendar"/> class.
    /// </summary>
    public Calendar()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Calendar"/> class with a specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the calendar.</param>
    public Calendar(Guid id) : base(id)
    {
    }

    /// <summary>
    /// Gets or initializes the synchronization configuration for this calendar.
    /// </summary>
    public required SyncConfiguration Configuration
    {
        get => _configuration;
        init => _configuration = value;
    }
    
    /// <summary>
    /// Gets the date and time of the last synchronization attempt.
    /// </summary>
    public DateTime? LastSyncAt { get; private set; }
    
    /// <summary>
    /// Enables the calendar for synchronization.
    /// </summary>
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
