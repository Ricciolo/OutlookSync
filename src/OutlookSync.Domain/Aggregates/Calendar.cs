using OutlookSync.Domain.Common;
using OutlookSync.Domain.Events;

namespace OutlookSync.Domain.Aggregates;

/// <summary>
/// Calendar aggregate - represents an Outlook calendar to sync
/// </summary>
public class Calendar : Entity, IAggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = [];
    
    public required string Name { get; init; }
    
    public required string ExternalId { get; init; }
    
    public required Guid DeviceId { get; init; }
    
    public string? Owner { get; init; }
    
    public bool IsEnabled { get; private set; } = true;
    
    public int TotalItemsSynced { get; private set; }
    
    public DateTime? LastSyncAt { get; private set; }
    
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public void Enable()
    {
        IsEnabled = true;
        MarkAsUpdated();
    }
    
    public void Disable()
    {
        IsEnabled = false;
        MarkAsUpdated();
    }
    
    public void RecordSuccessfulSync(int itemsSynced)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemsSynced, nameof(itemsSynced));
        
        LastSyncAt = DateTime.UtcNow;
        TotalItemsSynced += itemsSynced;
        
        MarkAsUpdated();
        _domainEvents.Add(new CalendarSyncedEvent(Id, DateTime.UtcNow, itemsSynced));
    }
    
    public void RecordFailedSync(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        
        LastSyncAt = DateTime.UtcNow;
        
        MarkAsUpdated();
        _domainEvents.Add(new CalendarSyncFailedEvent(Id, DateTime.UtcNow, reason));
    }
    
    public void ClearEvents() => _domainEvents.Clear();
}
