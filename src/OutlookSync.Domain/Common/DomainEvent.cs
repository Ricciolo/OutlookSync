namespace OutlookSync.Domain.Common;

/// <summary>
/// Base record for domain events
/// </summary>
public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
