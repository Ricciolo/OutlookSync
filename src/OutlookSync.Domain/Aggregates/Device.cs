using OutlookSync.Domain.Common;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Domain.Events;

namespace OutlookSync.Domain.Aggregates;

/// <summary>
/// Device aggregate - represents an authenticated device with token
/// </summary>
public class Device : Entity, IAggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = [];
    
    public required DeviceInfo Info { get; init; }
    
    public TokenStatus TokenStatus { get; private set; }
    
    public string? AccessToken { get; private set; }
    
    public DateTime? TokenAcquiredAt { get; private set; }
    
    public DateTime? TokenExpiresAt { get; private set; }
    
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public void AcquireToken(string accessToken, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));
        
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Token expiry must be in the future", nameof(expiresAt));
        
        AccessToken = accessToken;
        TokenAcquiredAt = DateTime.UtcNow;
        TokenExpiresAt = expiresAt;
        TokenStatus = TokenStatus.Valid;
        
        MarkAsUpdated();
        _domainEvents.Add(new TokenAcquiredEvent(Id, DateTime.UtcNow));
    }
    
    public void MarkTokenAsInvalid()
    {
        TokenStatus = TokenStatus.Invalid;
        MarkAsUpdated();
        _domainEvents.Add(new TokenExpiredEvent(Id, DateTime.UtcNow));
    }
    
    public void MarkTokenAsExpired()
    {
        TokenStatus = TokenStatus.Expired;
        MarkAsUpdated();
        _domainEvents.Add(new TokenExpiredEvent(Id, DateTime.UtcNow));
    }
    
    public bool IsTokenValid()
    {
        if (TokenStatus != TokenStatus.Valid)
            return false;
            
        if (TokenExpiresAt.HasValue && TokenExpiresAt.Value <= DateTime.UtcNow)
        {
            MarkTokenAsExpired();
            return false;
        }
        
        return true;
    }
    
    public void ClearEvents() => _domainEvents.Clear();
}
