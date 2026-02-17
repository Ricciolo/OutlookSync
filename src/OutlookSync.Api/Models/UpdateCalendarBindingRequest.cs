using System.ComponentModel.DataAnnotations;

namespace OutlookSync.Api.Models;

/// <summary>
/// Request model for updating an existing calendar binding
/// </summary>
public record UpdateCalendarBindingRequest
{
    /// <summary>
    /// Gets the name of the calendar binding
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets whether this binding should be enabled
    /// </summary>
    public bool IsEnabled { get; init; }
    
    /// <summary>
    /// Gets the synchronization configuration
    /// </summary>
    [Required(ErrorMessage = "Configuration is required")]
    public required CalendarBindingConfigurationDto Configuration { get; init; }
}
