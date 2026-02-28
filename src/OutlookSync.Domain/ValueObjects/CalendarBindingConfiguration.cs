namespace OutlookSync.Domain.ValueObjects;

/// <summary>
/// Defines how event title should be handled during synchronization
/// </summary>
public enum TitleHandling
{
    /// <summary>
    /// Copy the original title as-is
    /// </summary>
    Clone,
    
    /// <summary>
    /// Add a custom prefix before the original title
    /// </summary>
    Rename,
    
    /// <summary>
    /// Hide the original title and use a custom placeholder
    /// </summary>
    Hide
}

/// <summary>
/// Defines how reminders should be handled during synchronization
/// </summary>
public enum ReminderHandling
{
    /// <summary>
    /// Copy reminders to the target event
    /// </summary>
    Copy,
    
    /// <summary>
    /// Disable reminders on the target event
    /// </summary>
    Disable
}

/// <summary>
/// RSVP response status for event exclusion
/// </summary>
public enum RsvpResponse
{
    None,
    Yes,
    Maybe,
    No
}

/// <summary>
/// Event status for display and exclusion
/// </summary>
public enum EventStatus
{
    Free,
    Busy,
    Tentative,
    OutOfOffice,
    WorkingElsewhere
}

/// <summary>
/// Configuration for event exclusion rules based on RSVP response
/// </summary>
public record RsvpExclusionRule
{
    /// <summary>
    /// RSVP responses to exclude from synchronization
    /// </summary>
    public IReadOnlyCollection<RsvpResponse> ExcludedResponses { get; init; } = Array.Empty<RsvpResponse>();
    
    /// <summary>
    /// Creates a rule that excludes no RSVP responses (sync all)
    /// </summary>
    public static RsvpExclusionRule None() => new();
    
    /// <summary>
    /// Creates a rule that excludes specific RSVP responses
    /// </summary>
    public static RsvpExclusionRule Exclude(params RsvpResponse[] responses) => new()
    {
        ExcludedResponses = responses
    };
    
    /// <summary>
    /// Serializes excluded responses to a comma-separated string for storage
    /// </summary>
    public string ToSerializedString() => string.Join(",", ExcludedResponses);
    
    /// <summary>
    /// Deserializes excluded responses from a comma-separated string
    /// </summary>
    public static RsvpExclusionRule FromSerializedString(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return None();
        }
            
        var responses = serialized.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Enum.Parse<RsvpResponse>(s.Trim()))
            .ToArray();
            
        return Exclude(responses);
    }
}

/// <summary>
/// Configuration for event exclusion rules based on status
/// </summary>
public record StatusExclusionRule
{
    /// <summary>
    /// Event statuses to exclude from synchronization
    /// </summary>
    public IReadOnlyCollection<EventStatus> ExcludedStatuses { get; init; } = Array.Empty<EventStatus>();
    
    /// <summary>
    /// Creates a rule that excludes no statuses (sync all)
    /// </summary>
    public static StatusExclusionRule None() => new();
    
    /// <summary>
    /// Creates a rule that excludes specific statuses
    /// </summary>
    public static StatusExclusionRule Exclude(params EventStatus[] statuses) => new()
    {
        ExcludedStatuses = statuses
    };
    
    /// <summary>
    /// Serializes excluded statuses to a comma-separated string for storage
    /// </summary>
    public string ToSerializedString() => string.Join(",", ExcludedStatuses);
    
    /// <summary>
    /// Deserializes excluded statuses from a comma-separated string
    /// </summary>
    public static StatusExclusionRule FromSerializedString(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return None();
        }
            
        var statuses = serialized.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Enum.Parse<EventStatus>(s.Trim()))
            .ToArray();
            
        return Exclude(statuses);
    }
}

/// <summary>
/// Sync interval configuration
/// </summary>
public record SyncInterval
{
    public required int Minutes { get; init; }
    
    public string? CronExpression { get; init; }
    
    public static SyncInterval Every15Minutes() => new() { Minutes = 15, CronExpression = "*/15 * * * *" };
    
    public static SyncInterval Every30Minutes() => new() { Minutes = 30, CronExpression = "*/30 * * * *" };
    
    public static SyncInterval Hourly() => new() { Minutes = 60, CronExpression = "0 * * * *" };
    
    public static SyncInterval Custom(int minutes, string? cronExpression = null) => 
        new() { Minutes = minutes, CronExpression = cronExpression };
}

/// <summary>
/// Configuration for a calendar binding, defining how events are synchronized
/// from a source calendar to a target calendar
/// </summary>
public record CalendarBindingConfiguration
{
    /// <summary>
    /// How to handle event titles
    /// </summary>
    public TitleHandling TitleHandling { get; init; } = TitleHandling.Clone;
    
    /// <summary>
    /// Custom title prefix when using Rename, or full replacement text when using Hide
    /// </summary>
    public string? CustomTitle { get; init; }
    
    /// <summary>
    /// Whether to copy event description
    /// </summary>
    public bool CopyDescription { get; init; } = true;
    
    /// <summary>
    /// Whether to copy participants (as text, not as invited attendees)
    /// </summary>
    public bool CopyParticipants { get; init; } = true;
    
    /// <summary>
    /// Whether to copy location
    /// </summary>
    public bool CopyLocation { get; init; } = true;
    
    /// <summary>
    /// Category to apply to synchronized events
    /// </summary>
    public string? TargetCategory { get; init; }
    
    /// <summary>
    /// Status to apply to synchronized events (null means keep original)
    /// </summary>
    public EventStatus? TargetStatus { get; init; }
    
    /// <summary>
    /// Whether to copy attachments
    /// </summary>
    public bool CopyAttachments { get; init; }
    
    /// <summary>
    /// How to handle reminders
    /// </summary>
    public ReminderHandling ReminderHandling { get; init; } = ReminderHandling.Copy;
    
    /// <summary>
    /// Whether to mark synchronized events as private
    /// </summary>
    public bool MarkAsPrivate { get; init; }
    
    /// <summary>
    /// Custom tag to add to title or description of synchronized events
    /// </summary>
    public string? CustomTag { get; init; }
    
    /// <summary>
    /// Where to place the custom tag (in title or description)
    /// </summary>
    public bool CustomTagInTitle { get; init; } = true;
    
    /// <summary>
    /// Exclusion rule based on RSVP response
    /// </summary>
    public RsvpExclusionRule RsvpExclusion { get; init; } = RsvpExclusionRule.None();
    
    /// <summary>
    /// Exclusion rule based on event status
    /// </summary>
    public StatusExclusionRule StatusExclusion { get; init; } = StatusExclusionRule.None();
    
    /// <summary>
    /// Synchronization interval configuration
    /// </summary>
    public required SyncInterval Interval { get; init; }
    
    /// <summary>
    /// Gets the number of days forward to synchronize
    /// </summary>
    public int SyncDaysForward { get; init; } = 30;
    
    /// <summary>
    /// Creates a default configuration that copies all fields
    /// </summary>
    public static CalendarBindingConfiguration Default() => new() 
    { 
        Interval = SyncInterval.Every30Minutes() 
    };
    
    /// <summary>
    /// Creates a privacy-focused configuration that hides sensitive information
    /// </summary>
    public static CalendarBindingConfiguration PrivacyFocused() => new()
    {
        Interval = SyncInterval.Every30Minutes(),
        TitleHandling = TitleHandling.Hide,
        CustomTitle = "Busy",
        CopyDescription = false,
        CopyParticipants = false,
        CopyLocation = false,
        CopyAttachments = false,
        MarkAsPrivate = true,
        TargetStatus = EventStatus.Busy
    };
}
