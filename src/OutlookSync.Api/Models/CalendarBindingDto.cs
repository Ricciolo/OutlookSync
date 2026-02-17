using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Api.Models;

/// <summary>
/// Data transfer object for calendar binding
/// </summary>
public record CalendarBindingDto
{
    /// <summary>
    /// Gets the unique identifier of the calendar binding
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets the name of the calendar binding
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets the source credential identifier
    /// </summary>
    public Guid SourceCredentialId { get; init; }
    
    /// <summary>
    /// Gets the source calendar external identifier
    /// </summary>
    public required string SourceCalendarExternalId { get; init; }
    
    /// <summary>
    /// Gets the target credential identifier
    /// </summary>
    public Guid TargetCredentialId { get; init; }
    
    /// <summary>
    /// Gets the target calendar external identifier
    /// </summary>
    public required string TargetCalendarExternalId { get; init; }
    
    /// <summary>
    /// Gets whether this binding is enabled for synchronization
    /// </summary>
    public bool IsEnabled { get; init; }
    
    /// <summary>
    /// Gets the synchronization configuration
    /// </summary>
    public required CalendarBindingConfigurationDto Configuration { get; init; }
    
    /// <summary>
    /// Gets the date and time of the last synchronization attempt
    /// </summary>
    public DateTime? LastSyncAt { get; init; }
    
    /// <summary>
    /// Gets the number of events synchronized in the last successful sync
    /// </summary>
    public int LastSyncEventCount { get; init; }
    
    /// <summary>
    /// Gets the last synchronization error message, if any
    /// </summary>
    public string? LastSyncError { get; init; }
    
    /// <summary>
    /// Gets the date when the binding was created
    /// </summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Gets the date when the binding was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
    
    /// <summary>
    /// Converts domain entity to DTO
    /// </summary>
    public static CalendarBindingDto FromDomain(CalendarBinding binding) => new()
    {
        Id = binding.Id,
        Name = binding.Name,
        SourceCredentialId = binding.SourceCredentialId,
        SourceCalendarExternalId = binding.SourceCalendarExternalId,
        TargetCredentialId = binding.TargetCredentialId,
        TargetCalendarExternalId = binding.TargetCalendarExternalId,
        IsEnabled = binding.IsEnabled,
        Configuration = CalendarBindingConfigurationDto.FromDomain(binding.Configuration),
        LastSyncAt = binding.LastSyncAt,
        LastSyncEventCount = binding.LastSyncEventCount,
        LastSyncError = binding.LastSyncError,
        CreatedAt = binding.CreatedAt,
        UpdatedAt = binding.UpdatedAt
    };
}
