using OutlookSync.Domain.Common;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Aggregates;

/// <summary>
/// Calendar aggregate - represents an Outlook calendar to sync
/// </summary>
public class Calendar : Entity, IAggregateRoot
{
    private string _name = null!;
    
    /// <summary>
    /// Gets the name of the calendar.
    /// </summary>
    public required string Name 
    { 
        get => _name;
        init => _name = value;
    }
    
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
    
    /// <summary>
    /// Disables the calendar for synchronization.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Updates the synchronization configuration for this calendar.
    /// </summary>
    /// <param name="configuration">The new synchronization configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public void UpdateConfiguration(SyncConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        
        _configuration = configuration;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Renames the calendar.
    /// </summary>
    /// <param name="newName">The new name for the calendar.</param>
    /// <exception cref="ArgumentException">Thrown when newName is null, empty, or whitespace.</exception>
    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName, nameof(newName));
        
        _name = newName;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Records a successful synchronization attempt.
    /// </summary>
    /// <param name="itemsSynced">The number of items successfully synchronized.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when itemsSynced is negative.</exception>
    public void RecordSuccessfulSync(int itemsSynced)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemsSynced, nameof(itemsSynced));
        
        LastSyncAt = DateTime.UtcNow;
        
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Records a failed synchronization attempt.
    /// </summary>
    /// <param name="reason">The reason for the synchronization failure.</param>
    /// <exception cref="ArgumentException">Thrown when reason is null, empty, or whitespace.</exception>
    public void RecordFailedSync(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        
        LastSyncAt = DateTime.UtcNow;
        
        MarkAsUpdated();
    }
}
