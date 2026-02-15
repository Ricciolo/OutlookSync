using OutlookSync.Domain.Common;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Aggregates;

/// <summary>
/// Credential aggregate - represents authentication credentials from device code flow
/// </summary>
public class Credential : Entity, IAggregateRoot
{
    /// <summary>
    /// Gets the name of the credential.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets the current status of the token.
    /// </summary>
    public TokenStatus TokenStatus { get; private set; }
    
    /// <summary>
    /// Gets the access token.
    /// </summary>
    public string? AccessToken { get; private set; }
    
    /// <summary>
    /// Gets the refresh token.
    /// </summary>
    public string? RefreshToken { get; private set; }
    
    /// <summary>
    /// Gets the date and time when the token was acquired.
    /// </summary>
    public DateTime? TokenAcquiredAt { get; private set; }
    
    /// <summary>
    /// Gets the date and time when the token expires.
    /// </summary>
    public DateTime? TokenExpiresAt { get; private set; }
    
    /// <summary>
    /// Acquires a new token with the specified access token, refresh token, and expiration date.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="expiresAt">The expiration date and time.</param>
    /// <exception cref="ArgumentException">Thrown when access token or refresh token is null or whitespace, or when expiry date is not in the future.</exception>
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
