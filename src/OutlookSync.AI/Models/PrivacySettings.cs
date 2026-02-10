namespace OutlookSync.AI.Models;

/// <summary>
/// Represents the privacy settings for the AI module.
/// </summary>
public sealed class PrivacySettings
{
    /// <summary>
    /// Gets or sets a value indicating whether personal data can be sent to the AI service.
    /// </summary>
    public bool AllowPersonalData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether calendar data can be shared with the AI service.
    /// </summary>
    public bool AllowCalendarDataSharing { get; set; }

    /// <summary>
    /// Gets or sets the data retention period in days. Zero means no retention.
    /// </summary>
    public int DataRetentionDays { get; set; }
}
