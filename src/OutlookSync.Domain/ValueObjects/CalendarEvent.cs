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

    public string ExternalId { get; set; } = string.Empty;

    public required string Subject { get; init; }

    public DateTime Start { get; init; }

    public DateTime End { get; init; }

    public string? Location { get; init; }

    /// <summary>
    /// Whether the event is an online meeting
    /// </summary>
    public bool IsOnlineMeeting { get; init; }

    /// <summary>
    /// Whether the event is a meeting with attendees
    /// </summary>
    public bool IsMeeting { get; init; }

    public string? Body { get; init; }

    /// <summary>
    /// Type of the body content (Text or Html)
    /// </summary>
    public CalendarEventBodyType BodyType { get; init; } = CalendarEventBodyType.Text;

    public string? Organizer { get; init; }

    public bool IsAllDay { get; init; }

    public bool IsRecurring { get; init; }

    /// <summary>
    /// Event status (Busy, Free, etc.)
    /// </summary>
    public EventStatus Status { get; init; } = EventStatus.Busy;

    /// <summary>
    /// RSVP response status
    /// </summary>
    public RsvpResponse RsvpStatus { get; init; } = RsvpResponse.None;

    /// <summary>
    /// Required attendees (email addresses)
    /// </summary>
    public IReadOnlyList<string> RequiredAttendees { get; init; } = [];

    /// <summary>
    /// Optional attendees (email addresses)
    /// </summary>
    public IReadOnlyList<string> OptionalAttendees { get; init; } = [];

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
    /// Minutes before the event start when the reminder should trigger (null if no reminder)
    /// </summary>
    public int? ReminderMinutesBeforeStart { get; init; }

    /// <summary>
    /// Indicates if this event was copied from another calendar
    /// </summary>
    public bool IsCopiedEvent => !string.IsNullOrWhiteSpace(OriginalEventId);

    /// <summary>
    /// Original event external ID if this is a copied event
    /// </summary>
    public string? OriginalEventId { get; init; }

    /// <summary>
    /// Source calendar binding ID if this is a copied event
    /// </summary>
    public Guid? SourceCalendarBindingId { get; init; }    
}
