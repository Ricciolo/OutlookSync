using OutlookSync.Domain.Common;

namespace OutlookSync.Domain.Events;

/// <summary>
/// Event raised when a token expires
/// </summary>
public record TokenExpiredEvent(
    Guid DeviceId,
    DateTime ExpiredAt) : DomainEvent;
