using Microsoft.AspNetCore.Components;

namespace OutlookSync.Web.Components.Shared;

/// <summary>
/// Model for calendar settings form
/// </summary>
public class CalendarSettingsModel
{
    /// <summary>
    /// Gets or sets the calendar name
    /// </summary>
    public string CalendarName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the sync interval in minutes
    /// </summary>
    public int SelectedInterval { get; set; } = 30;
    
    /// <summary>
    /// Gets or sets the start date for synchronization
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.Today;
    
    /// <summary>
    /// Gets or sets the number of days forward to synchronize
    /// </summary>
    public int SyncDaysForward { get; set; } = 30;
    
    /// <summary>
    /// Gets or sets whether events should be marked as private
    /// </summary>
    public bool IsPrivate { get; set; }
    
    /// <summary>
    /// Gets or sets the field selection type
    /// </summary>
    public string FieldSelectionType { get; set; } = "all";
}

/// <summary>
/// Reusable component for calendar configuration settings
/// </summary>
public partial class CalendarSettingsForm
{
    /// <summary>
    /// Gets or sets the calendar settings model
    /// </summary>
    [Parameter]
    public CalendarSettingsModel Settings { get; set; } = new();
}
