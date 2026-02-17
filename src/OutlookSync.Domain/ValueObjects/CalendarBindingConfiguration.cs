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
    /// Rename the title with a custom prefix/suffix
    /// </summary>
    Rename,
    
    /// <summary>
    /// Hide the original title and use a generic placeholder
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
    Disable,
    
    /// <summary>
    /// Remove reminders from the original event (move to copy only)
    /// </summary>
    Move
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
/// Calendar event color for filtering
/// </summary>
public enum EventColor
{
    None,
    Red,
    Orange,
    Yellow,
    Green,
    Blue,
    Purple,
    Pink,
    Brown,
    Gray,
    Black
}

/// <summary>
/// Configuration for event exclusion rules based on color
/// </summary>
public record ColorExclusionRule
{
    /// <summary>
    /// Colors to exclude from synchronization
    /// </summary>
    public IReadOnlyCollection<EventColor> ExcludedColors { get; init; } = Array.Empty<EventColor>();
    
    /// <summary>
    /// Creates a rule that excludes no colors (sync all)
    /// </summary>
    public static ColorExclusionRule None() => new();
    
    /// <summary>
    /// Creates a rule that excludes specific colors
    /// </summary>
    public static ColorExclusionRule Exclude(params EventColor[] colors) => new()
    {
        ExcludedColors = colors
    };
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
    /// Custom title prefix or full replacement when using Rename or Hide
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
    /// Whether to copy conference link
    /// </summary>
    public bool CopyConferenceLink { get; init; } = true;
    
    /// <summary>
    /// Event color to apply to synchronized events (None means keep original)
    /// </summary>
    public EventColor? TargetEventColor { get; init; }
    
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
    public bool CopyAttachments { get; init; } = false;
    
    /// <summary>
    /// How to handle reminders
    /// </summary>
    public ReminderHandling ReminderHandling { get; init; } = ReminderHandling.Copy;
    
    /// <summary>
    /// Whether to mark synchronized events as private
    /// </summary>
    public bool MarkAsPrivate { get; init; } = false;
    
    /// <summary>
    /// Custom tag to add to title or description of synchronized events
    /// </summary>
    public string? CustomTag { get; init; }
    
    /// <summary>
    /// Where to place the custom tag (in title or description)
    /// </summary>
    public bool CustomTagInTitle { get; init; } = true;
    
    /// <summary>
    /// Exclusion rule based on event color
    /// </summary>
    public ColorExclusionRule ColorExclusion { get; init; } = ColorExclusionRule.None();
    
    /// <summary>
    /// Exclusion rule based on RSVP response
    /// </summary>
    public RsvpExclusionRule RsvpExclusion { get; init; } = RsvpExclusionRule.None();
    
    /// <summary>
    /// Exclusion rule based on event status
    /// </summary>
    public StatusExclusionRule StatusExclusion { get; init; } = StatusExclusionRule.None();
    
    /// <summary>
    /// Creates a default configuration that copies all fields
    /// </summary>
    public static CalendarBindingConfiguration Default() => new();
    
    /// <summary>
    /// Creates a privacy-focused configuration that hides sensitive information
    /// </summary>
    public static CalendarBindingConfiguration PrivacyFocused() => new()
    {
        TitleHandling = TitleHandling.Hide,
        CustomTitle = "Busy",
        CopyDescription = false,
        CopyParticipants = false,
        CopyLocation = false,
        CopyConferenceLink = false,
        CopyAttachments = false,
        MarkAsPrivate = true,
        TargetStatus = EventStatus.Busy
    };
}
