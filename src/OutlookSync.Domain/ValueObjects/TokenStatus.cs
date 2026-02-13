namespace OutlookSync.Domain.ValueObjects;

/// <summary>
/// Value object representing token status
/// </summary>
public enum TokenStatus
{
    NotAcquired = 0,
    Valid = 1,
    Invalid = 2,
    Expired = 3
}
