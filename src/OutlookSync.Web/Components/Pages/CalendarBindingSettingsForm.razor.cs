using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Code-behind for CalendarBindingSettingsForm component
/// </summary>
public partial class CalendarBindingSettingsForm
{
    private TitleHandling _titleHandling = TitleHandling.Clone;
    private string _customTitle = string.Empty;
    private bool _copyDescription = true;
    private bool _copyParticipants = true;
    private bool _copyLocation = true;
    private bool _copyConferenceLink = true;
    private bool _copyAttachments = false;
    private string _targetEventColor = string.Empty;
    private string _targetCategory = string.Empty;
    private string _targetStatus = string.Empty;
    private ReminderHandling _reminderHandling = ReminderHandling.Copy;
    private bool _markAsPrivate = false;
    private string _customTag = string.Empty;
    private bool _customTagInTitle = true;
    private string _colorExclusion = string.Empty;
    private string _rsvpExclusion = string.Empty;
    private string _statusExclusion = string.Empty;

    /// <summary>
    /// Gets the configuration from the form
    /// </summary>
    public CalendarBindingConfiguration GetConfiguration()
    {
        return new CalendarBindingConfiguration
        {
            TitleHandling = _titleHandling,
            CustomTitle = string.IsNullOrWhiteSpace(_customTitle) ? null : _customTitle.Trim(),
            CopyDescription = _copyDescription,
            CopyParticipants = _copyParticipants,
            CopyLocation = _copyLocation,
            CopyConferenceLink = _copyConferenceLink,
            CopyAttachments = _copyAttachments,
            TargetEventColor = ParseEventColor(_targetEventColor),
            TargetCategory = string.IsNullOrWhiteSpace(_targetCategory) ? null : _targetCategory.Trim(),
            TargetStatus = ParseEventStatus(_targetStatus),
            ReminderHandling = _reminderHandling,
            MarkAsPrivate = _markAsPrivate,
            CustomTag = string.IsNullOrWhiteSpace(_customTag) ? null : _customTag.Trim(),
            CustomTagInTitle = _customTagInTitle,
            ColorExclusion = ColorExclusionRule.FromSerializedString(_colorExclusion),
            RsvpExclusion = RsvpExclusionRule.FromSerializedString(_rsvpExclusion),
            StatusExclusion = StatusExclusionRule.FromSerializedString(_statusExclusion)
        };
    }

    /// <summary>
    /// Sets the configuration in the form
    /// </summary>
    public void SetConfiguration(CalendarBindingConfiguration configuration)
    {
        _titleHandling = configuration.TitleHandling;
        _customTitle = configuration.CustomTitle ?? string.Empty;
        _copyDescription = configuration.CopyDescription;
        _copyParticipants = configuration.CopyParticipants;
        _copyLocation = configuration.CopyLocation;
        _copyConferenceLink = configuration.CopyConferenceLink;
        _copyAttachments = configuration.CopyAttachments;
        _targetEventColor = configuration.TargetEventColor?.ToString() ?? string.Empty;
        _targetCategory = configuration.TargetCategory ?? string.Empty;
        _targetStatus = configuration.TargetStatus?.ToString() ?? string.Empty;
        _reminderHandling = configuration.ReminderHandling;
        _markAsPrivate = configuration.MarkAsPrivate;
        _customTag = configuration.CustomTag ?? string.Empty;
        _customTagInTitle = configuration.CustomTagInTitle;
        _colorExclusion = configuration.ColorExclusion.ToSerializedString();
        _rsvpExclusion = configuration.RsvpExclusion.ToSerializedString();
        _statusExclusion = configuration.StatusExclusion.ToSerializedString();
        
        StateHasChanged();
    }

    private static EventColor? ParseEventColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<EventColor>(value, out var color) ? color : null;
    }

    private static EventStatus? ParseEventStatus(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<EventStatus>(value, out var status) ? status : null;
    }
}
