using OutlookSync.Domain.Common;

namespace OutlookSync.Domain.Events;

/// <summary>
/// Event raised when calendar sync fails
/// </summary>
public record CalendarSyncFailedEvent(
    Guid CalendarId,
    DateTime FailedAt,
    string Reason) : DomainEvent;
