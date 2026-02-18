using OutlookSync.Domain.Common;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Aggregates;

/// <summary>
/// CalendarBinding aggregate - represents a unidirectional synchronization relationship
/// between a source calendar and a target calendar with specific configuration
/// </summary>
public class CalendarBinding : Entity, IAggregateRoot
{
    private string _name = null!;
    private CalendarBindingConfiguration _configuration = null!;
    
    /// <summary>
    /// Gets the name of the calendar binding.
    /// </summary>
    public required string Name 
    { 
        get => _name;
        init => _name = value;
    }
    
    /// <summary>
    /// Gets the source credential identifier (credential for accessing the source calendar).
    /// </summary>
    public required Guid SourceCredentialId { get; init; }
    
    /// <summary>
    /// Gets the source calendar external identifier (from Exchange/Outlook).
    /// </summary>
    public required string SourceCalendarExternalId { get; init; }
    
    /// <summary>
    /// Gets the target credential identifier (credential for accessing the target calendar).
    /// </summary>
    public required Guid TargetCredentialId { get; init; }
    
    /// <summary>
    /// Gets the target calendar external identifier (from Exchange/Outlook).
    /// </summary>
    public required string TargetCalendarExternalId { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether this binding is enabled for synchronization.
    /// </summary>
    public bool IsEnabled { get; private set; } = true;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarBinding"/> class.
    /// </summary>
    public CalendarBinding()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarBinding"/> class with a specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the calendar binding.</param>
    public CalendarBinding(Guid id) : base(id)
    {
    }
    
    /// <summary>
    /// Gets or initializes the synchronization configuration for this binding.
    /// </summary>
    public required CalendarBindingConfiguration Configuration
    {
        get => _configuration;
        init => _configuration = value;
    }
    
    /// <summary>
    /// Gets the date and time of the last synchronization attempt for this binding.
    /// </summary>
    public DateTime? LastSyncAt { get; private set; }
    
    /// <summary>
    /// Gets the number of events synchronized in the last successful sync.
    /// </summary>
    public int LastSyncEventCount { get; private set; }
    
    /// <summary>
    /// Gets the last synchronization error message, if any.
    /// </summary>
    public string? LastSyncError { get; private set; }
    
    /// <summary>
    /// Enables the calendar binding for synchronization.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Disables the calendar binding for synchronization.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Updates the synchronization configuration for this binding.
    /// </summary>
    /// <param name="configuration">The new synchronization configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public void UpdateConfiguration(CalendarBindingConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        
        _configuration = configuration;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Renames the calendar binding.
    /// </summary>
    /// <param name="newName">The new name for the binding.</param>
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
    /// <param name="eventCount">The number of events successfully synchronized.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when eventCount is negative.</exception>
    public void RecordSuccessfulSync(int eventCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(eventCount, nameof(eventCount));
        
        LastSyncAt = DateTime.UtcNow;
        LastSyncEventCount = eventCount;
        LastSyncError = null;
        
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Records a failed synchronization attempt.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <exception cref="ArgumentException">Thrown when errorMessage is null, empty, or whitespace.</exception>
    public void RecordFailedSync(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));
        
        LastSyncAt = DateTime.UtcNow;
        LastSyncError = errorMessage;
        
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Validates that this binding does not create a duplicate source-target pair.
    /// </summary>
    /// <param name="sourceCredentialId">Source credential ID to validate.</param>
    /// <param name="sourceExternalId">Source calendar external ID to validate.</param>
    /// <param name="targetCredentialId">Target credential ID to validate.</param>
    /// <param name="targetExternalId">Target calendar external ID to validate.</param>
    /// <returns>True if the binding is valid (not a duplicate), false otherwise.</returns>
    public bool IsValidBinding(Guid sourceCredentialId, string sourceExternalId, Guid targetCredentialId, string targetExternalId)
    {
        // Same credential and calendar cannot be both source and target
        if (sourceCredentialId == targetCredentialId && sourceExternalId == targetExternalId)
        {
            return false;
        }
            
        // Check if this creates a duplicate binding
        return !(SourceCredentialId == sourceCredentialId && 
                 SourceCalendarExternalId == sourceExternalId && 
                 TargetCredentialId == targetCredentialId && 
                 TargetCalendarExternalId == targetExternalId);
    }
    
    /// <summary>
    /// Checks if the binding represents the reverse of another binding (target→source becomes source→target).
    /// </summary>
    /// <param name="other">The other binding to compare with.</param>
    /// <returns>True if this binding is the reverse of the other binding.</returns>
    public bool IsReverseOf(CalendarBinding other)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));
        
        return SourceCredentialId == other.TargetCredentialId && 
               SourceCalendarExternalId == other.TargetCalendarExternalId &&
               TargetCredentialId == other.SourceCredentialId && 
               TargetCalendarExternalId == other.SourceCalendarExternalId;
    }
}
