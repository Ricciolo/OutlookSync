using OutlookSync.Domain.Common;

namespace OutlookSync.Domain.Events;

/// <summary>
/// Event raised when a calendar is successfully synced
/// </summary>
public record CalendarSyncedEvent(
    Guid CalendarId,
    DateTime SyncedAt,
    int ItemsSynced) : DomainEvent;
