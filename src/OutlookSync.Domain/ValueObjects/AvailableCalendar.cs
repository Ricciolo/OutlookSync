namespace OutlookSync.Domain.ValueObjects;

/// <summary>
/// Represents a calendar that is available for synchronization
/// </summary>
public record AvailableCalendar
{
    /// <summary>
    /// External identifier for the calendar (e.g., Exchange folder ID)
    /// </summary>
    public required string ExternalId { get; init; }
    
    /// <summary>
    /// Display name of the calendar
    /// </summary>
    public required string Name { get; init; }
}
