using System.ComponentModel.DataAnnotations;

namespace OutlookSync.Api.Models;

/// <summary>
/// Request model for creating a new calendar binding
/// </summary>
public record CreateCalendarBindingRequest
{
    /// <summary>
    /// Gets the name of the calendar binding
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets the source credential identifier
    /// </summary>
    [Required(ErrorMessage = "Source credential ID is required")]
    public required Guid SourceCredentialId { get; init; }
    
    /// <summary>
    /// Gets the source calendar external identifier
    /// </summary>
    [Required(ErrorMessage = "Source calendar external ID is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Source calendar external ID must be between 1 and 500 characters")]
    public required string SourceCalendarExternalId { get; init; }
    
    /// <summary>
    /// Gets the target credential identifier
    /// </summary>
    [Required(ErrorMessage = "Target credential ID is required")]
    public required Guid TargetCredentialId { get; init; }
    
    /// <summary>
    /// Gets the target calendar external identifier
    /// </summary>
    [Required(ErrorMessage = "Target calendar external ID is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Target calendar external ID must be between 1 and 500 characters")]
    public required string TargetCalendarExternalId { get; init; }
    
    /// <summary>
    /// Gets whether this binding should be enabled initially (defaults to true)
    /// </summary>
    public bool IsEnabled { get; init; } = true;
    
    /// <summary>
    /// Gets the synchronization configuration
    /// </summary>
    [Required(ErrorMessage = "Configuration is required")]
    public required CalendarBindingConfigurationDto Configuration { get; init; }
}
