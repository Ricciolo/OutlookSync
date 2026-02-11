namespace OutlookSync.Application.DTOs;

/// <summary>
/// DTO for calendar items from Exchange
/// </summary>
public record CalendarItemDto
{
    public required string Id { get; init; }
    
    public required string Subject { get; init; }
    
    public DateTime Start { get; init; }
    
    public DateTime End { get; init; }
    
    public string? Location { get; init; }
    
    public string? Body { get; init; }
    
    public string? Organizer { get; init; }
    
    public IEnumerable<string> Attendees { get; init; } = [];
    
    public bool IsAllDay { get; init; }
    
    public bool IsRecurring { get; init; }
}
