namespace OutlookSync.Domain.ValueObjects;

/// <summary>
/// Body type for calendar events
/// </summary>
public enum CalendarEventBodyType
{
    Text,
    Html
}

/// <summary>
/// Calendar event value object - represents an event within a calendar
/// </summary>
public record CalendarEvent
{
    public required Guid Id { get; init; }
    
    public required Guid CalendarId { get; init; }

    public string ExternalId { get; set; } = string.Empty;

    public required string Subject { get; init; }
    
    public DateTime Start { get; init; }
    
    public DateTime End { get; init; }
    
    public string? Location { get; init; }
    
    public string? Body { get; init; }
    
    /// <summary>
    /// Type of the body content (Text or Html)
    /// </summary>
    public CalendarEventBodyType BodyType { get; init; } = CalendarEventBodyType.Text;
    
    public string? Organizer { get; init; }
    
    public bool IsAllDay { get; init; }
    
    public bool IsRecurring { get; init; }
    
    /// <summary>
    /// Event color
    /// </summary>
    public EventColor Color { get; init; } = EventColor.None;
    
    /// <summary>
    /// Event status (Busy, Free, etc.)
    /// </summary>
    public EventStatus Status { get; init; } = EventStatus.Busy;
    
    /// <summary>
    /// RSVP response status
    /// </summary>
    public RsvpResponse RsvpStatus { get; init; } = RsvpResponse.None;
    
    /// <summary>
    /// List of attendees as a text string
    /// </summary>
    public string? Attendees { get; init; }
    
    /// <summary>
    /// Conference link (Teams, Zoom, etc.)
    /// </summary>
    public string? ConferenceLink { get; init; }
    
    /// <summary>
    /// Categories assigned to the event
    /// </summary>
    public string? Categories { get; init; }
    
    /// <summary>
    /// Whether the event is marked as private
    /// </summary>
    public bool IsPrivate { get; init; }
    
    /// <summary>
    /// Whether the event has attachments
    /// </summary>
    public bool HasAttachments { get; init; }
    
    /// <summary>
    /// Whether the event has reminders enabled
    /// </summary>
    public bool HasReminders { get; init; }

    /// <summary>
    /// Indicates if this event was copied from another calendar
    /// </summary>
    public bool IsCopiedEvent => !string.IsNullOrWhiteSpace(OriginalEventId);
    
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
            OriginalEventId = originalEventId,
            SourceCalendarId = sourceCalendarId
        };
    }
    
    /// <summary>
    /// Creates a new event with updated details
    /// </summary>
    public CalendarEvent WithUpdatedDetails(
        string subject, 
        DateTime start, 
        DateTime end, 
        string? location = null, 
        string? body = null,
        CalendarEventBodyType bodyType = CalendarEventBodyType.Text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject, nameof(subject));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(start, end, nameof(start));
        
        return this with
        {
            Subject = subject,
            Start = start,
            End = end,
            Location = location,
            Body = body,
            BodyType = bodyType
        };
    }
}
