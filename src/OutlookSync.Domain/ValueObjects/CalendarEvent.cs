namespace OutlookSync.Domain.ValueObjects;

/// <summary>
/// Calendar event value object - represents an event within a calendar
/// </summary>
public record CalendarEvent
{
    public required Guid Id { get; init; }
    
    public required Guid CalendarId { get; init; }
    
    public required string ExternalId { get; init; }
    
    public required string Subject { get; init; }
    
    public DateTime Start { get; init; }
    
    public DateTime End { get; init; }
    
    public string? Location { get; init; }
    
    public string? Body { get; init; }
    
    public string? Organizer { get; init; }
    
    public bool IsAllDay { get; init; }
    
    public bool IsRecurring { get; init; }
    
    /// <summary>
    /// Indicates if this event was copied from another calendar
    /// </summary>
    public bool IsCopiedEvent { get; init; }
    
    /// <summary>
    /// Original event external ID if this is a copied event
    /// </summary>
    public string? OriginalEventId { get; init; }
    
    /// <summary>
    /// Source calendar ID if this is a copied event
    /// </summary>
    public Guid? SourceCalendarId { get; init; }
    
    /// <summary>
    /// Creates a copied version of this event for a target calendar
    /// </summary>
    public CalendarEvent AsCopiedEvent(Guid targetCalendarId, string newExternalId, string originalEventId, Guid sourceCalendarId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newExternalId, nameof(newExternalId));
        ArgumentException.ThrowIfNullOrWhiteSpace(originalEventId, nameof(originalEventId));
        
        return this with
        {
            Id = Guid.NewGuid(),
            CalendarId = targetCalendarId,
            ExternalId = newExternalId,
            IsCopiedEvent = true,
            OriginalEventId = originalEventId,
            SourceCalendarId = sourceCalendarId
        };
    }
    
    /// <summary>
    /// Creates a new event with updated details
    /// </summary>
    public CalendarEvent WithUpdatedDetails(string subject, DateTime start, DateTime end, string? location = null, string? body = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject, nameof(subject));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(start, end, nameof(start));
        
        return this with
        {
            Subject = subject,
            Start = start,
            End = end,
            Location = location,
            Body = body
        };
    }
}
