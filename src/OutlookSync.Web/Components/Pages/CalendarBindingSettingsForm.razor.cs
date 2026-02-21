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
    private bool _copyAttachments;
    private string _targetCategory = string.Empty;
    private string _targetStatus = string.Empty;
    private ReminderHandling _reminderHandling = ReminderHandling.Copy;
    private bool _markAsPrivate;
    private string _customTag = string.Empty;
    private bool _customTagInTitle = true;
    private string _rsvpExclusion = string.Empty;
    private string _statusExclusion = string.Empty;
    private string _syncIntervalPreset = "30";
    private int _syncIntervalMinutes = 30;
    private int _syncDaysForward = 30;

    /// <summary>
    /// Gets the configuration from the form
    /// </summary>
    public CalendarBindingConfiguration GetConfiguration()
    {
        var syncInterval = _syncIntervalPreset switch
        {
            "15" => SyncInterval.Every15Minutes(),
            "30" => SyncInterval.Every30Minutes(),
            "60" => SyncInterval.Hourly(),
            "custom" => SyncInterval.Custom(_syncIntervalMinutes),
            _ => SyncInterval.Every30Minutes()
        };

        return new CalendarBindingConfiguration
        {
            Interval = syncInterval,
            SyncDaysForward = _syncDaysForward,
            TitleHandling = _titleHandling,
            CustomTitle = string.IsNullOrWhiteSpace(_customTitle) ? null : _customTitle.Trim(),
            CopyDescription = _copyDescription,
            CopyParticipants = _copyParticipants,
            CopyLocation = _copyLocation,
            CopyAttachments = _copyAttachments,
            TargetCategory = string.IsNullOrWhiteSpace(_targetCategory) ? null : _targetCategory.Trim(),
            TargetStatus = ParseEventStatus(_targetStatus),
            ReminderHandling = _reminderHandling,
            MarkAsPrivate = _markAsPrivate,
            CustomTag = string.IsNullOrWhiteSpace(_customTag) ? null : _customTag.Trim(),
            CustomTagInTitle = _customTagInTitle,
            RsvpExclusion = RsvpExclusionRule.FromSerializedString(_rsvpExclusion),
            StatusExclusion = StatusExclusionRule.FromSerializedString(_statusExclusion)
        };
    }

    /// <summary>
    /// Sets the configuration in the form
    /// </summary>
    public void SetConfiguration(CalendarBindingConfiguration configuration)
    {
        // Set sync interval
        _syncIntervalMinutes = configuration.Interval.Minutes;
        _syncIntervalPreset = configuration.Interval.Minutes switch
        {
            15 => "15",
            30 => "30",
            60 => "60",
            _ => "custom"
        };
        _syncDaysForward = configuration.SyncDaysForward;

        _titleHandling = configuration.TitleHandling;
        _customTitle = configuration.CustomTitle ?? string.Empty;
        _copyDescription = configuration.CopyDescription;
        _copyParticipants = configuration.CopyParticipants;
        _copyLocation = configuration.CopyLocation;
        _copyAttachments = configuration.CopyAttachments;
        _targetCategory = configuration.TargetCategory ?? string.Empty;
        _targetStatus = configuration.TargetStatus?.ToString() ?? string.Empty;
        _reminderHandling = configuration.ReminderHandling;
        _markAsPrivate = configuration.MarkAsPrivate;
        _customTag = configuration.CustomTag ?? string.Empty;
        _customTagInTitle = configuration.CustomTagInTitle;
        _rsvpExclusion = configuration.RsvpExclusion.ToSerializedString();
        _statusExclusion = configuration.StatusExclusion.ToSerializedString();
        
        StateHasChanged();
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
