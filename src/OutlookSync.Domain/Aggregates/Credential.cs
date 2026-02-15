using OutlookSync.Domain.Common;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Aggregates;

/// <summary>
/// Credential aggregate - represents authentication credentials from device code flow
/// </summary>
public class Credential : Entity, IAggregateRoot
{
    /// <summary>
    /// Gets the current status of the token.
    /// </summary>
    public TokenStatus TokenStatus { get; private set; }
    
    /// <summary>
    /// Gets the serialized status data.
    /// </summary>
    public byte[]? StatusData { get; private set; }
        
    /// <summary>
    /// Updates the credential status data.
    /// </summary>
    /// <param name="statusData">The serialized status data.</param>
    /// <exception cref="ArgumentNullException">Thrown when status data is null.</exception>
    public void UpdateStatusData(byte[] statusData)
    {
        ArgumentNullException.ThrowIfNull(statusData, nameof(statusData));
        
        StatusData = statusData;
        TokenStatus = TokenStatus.Valid;
        
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Marks the token as invalid.
    /// </summary>
    public void MarkTokenAsInvalid()
    {
        TokenStatus = TokenStatus.Invalid;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Marks the token as expired.
    /// </summary>
    public void MarkTokenAsExpired()
    {
        TokenStatus = TokenStatus.Expired;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Checks if the token is currently valid.
    /// </summary>
    /// <returns>True if the token status is Valid; otherwise, false.</returns>
    public bool IsTokenValid() => TokenStatus == TokenStatus.Valid;
}
