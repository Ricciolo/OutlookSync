using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Api.Models;

/// <summary>
/// Data transfer object for calendar binding configuration
/// </summary>
public record CalendarBindingConfigurationDto
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
    /// Event color to apply to synchronized events (null means keep original)
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
    /// Excluded event colors (comma-separated)
    /// </summary>
    public string? ExcludedColors { get; init; }
    
    /// <summary>
    /// Excluded RSVP responses (comma-separated)
    /// </summary>
    public string? ExcludedRsvpResponses { get; init; }
    
    /// <summary>
    /// Excluded event statuses (comma-separated)
    /// </summary>
    public string? ExcludedStatuses { get; init; }
    
    /// <summary>
    /// Converts domain configuration to DTO
    /// </summary>
    public static CalendarBindingConfigurationDto FromDomain(CalendarBindingConfiguration config) => new()
    {
        TitleHandling = config.TitleHandling,
        CustomTitle = config.CustomTitle,
        CopyDescription = config.CopyDescription,
        CopyParticipants = config.CopyParticipants,
        CopyLocation = config.CopyLocation,
        CopyConferenceLink = config.CopyConferenceLink,
        TargetEventColor = config.TargetEventColor,
        TargetCategory = config.TargetCategory,
        TargetStatus = config.TargetStatus,
        CopyAttachments = config.CopyAttachments,
        ReminderHandling = config.ReminderHandling,
        MarkAsPrivate = config.MarkAsPrivate,
        CustomTag = config.CustomTag,
        CustomTagInTitle = config.CustomTagInTitle,
        ExcludedColors = config.ColorExclusion.ToSerializedString(),
        ExcludedRsvpResponses = config.RsvpExclusion.ToSerializedString(),
        ExcludedStatuses = config.StatusExclusion.ToSerializedString()
    };
    
    /// <summary>
    /// Converts DTO to domain configuration
    /// </summary>
    public CalendarBindingConfiguration ToDomain() => new()
    {
        TitleHandling = TitleHandling,
        CustomTitle = CustomTitle,
        CopyDescription = CopyDescription,
        CopyParticipants = CopyParticipants,
        CopyLocation = CopyLocation,
        CopyConferenceLink = CopyConferenceLink,
        TargetEventColor = TargetEventColor,
        TargetCategory = TargetCategory,
        TargetStatus = TargetStatus,
        CopyAttachments = CopyAttachments,
        ReminderHandling = ReminderHandling,
        MarkAsPrivate = MarkAsPrivate,
        CustomTag = CustomTag,
        CustomTagInTitle = CustomTagInTitle,
        ColorExclusion = ColorExclusionRule.FromSerializedString(ExcludedColors),
        RsvpExclusion = RsvpExclusionRule.FromSerializedString(ExcludedRsvpResponses),
        StatusExclusion = StatusExclusionRule.FromSerializedString(ExcludedStatuses)
    };
}
