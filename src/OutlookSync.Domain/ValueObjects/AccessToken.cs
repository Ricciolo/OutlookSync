namespace OutlookSync.Domain.ValueObjects;

/// <summary>
/// Represents an OAuth access token with expiration
/// </summary>
public record AccessToken
{
    /// <summary>
    /// The access token string
    /// </summary>
    public required string Token { get; init; }
    
    /// <summary>
    /// When the token expires
    /// </summary>
    public required DateTimeOffset ExpiresOn { get; init; }
    
    /// <summary>
    /// Checks if the token is expired
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresOn;
    
    /// <summary>
    /// Checks if the token will expire within the specified time span
    /// </summary>
    public bool ExpiresWithin(TimeSpan timeSpan) => 
        DateTimeOffset.UtcNow.Add(timeSpan) >= ExpiresOn;
}
