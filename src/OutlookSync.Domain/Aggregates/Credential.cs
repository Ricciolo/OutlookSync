using OutlookSync.Domain.Common;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Aggregates;

/// <summary>
/// Credential aggregate - represents authentication credentials from device code flow
/// </summary>
public class Credential : Entity, IAggregateRoot
{
    public required string Name { get; init; }
    
    public TokenStatus TokenStatus { get; private set; }
    
    public string? AccessToken { get; private set; }
    
    public string? RefreshToken { get; private set; }
    
    public DateTime? TokenAcquiredAt { get; private set; }
    
    public DateTime? TokenExpiresAt { get; private set; }
    
    public void AcquireToken(string accessToken, string refreshToken, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, nameof(refreshToken));
        
        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Token expiry must be in the future", nameof(expiresAt));
        }
        
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenAcquiredAt = DateTime.UtcNow;
        TokenExpiresAt = expiresAt;
        TokenStatus = TokenStatus.Valid;
        
        MarkAsUpdated();
    }
    
    public void MarkTokenAsInvalid()
    {
        TokenStatus = TokenStatus.Invalid;
        MarkAsUpdated();
    }
    
    public void MarkTokenAsExpired()
    {
        TokenStatus = TokenStatus.Expired;
        MarkAsUpdated();
    }
    
    public bool IsTokenValid()
    {
        if (TokenStatus != TokenStatus.Valid)
        {
            return false;
        }
            
        if (TokenExpiresAt.HasValue && TokenExpiresAt.Value <= DateTime.UtcNow)
        {
            MarkTokenAsExpired();
            return false;
        }
        
        return true;
    }
}
