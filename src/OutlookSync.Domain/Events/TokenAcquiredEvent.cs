using OutlookSync.Domain.Common;

namespace OutlookSync.Domain.Events;

/// <summary>
/// Event raised when a token is successfully acquired
/// </summary>
public record TokenAcquiredEvent(
    Guid DeviceId,
    DateTime AcquiredAt) : DomainEvent;
