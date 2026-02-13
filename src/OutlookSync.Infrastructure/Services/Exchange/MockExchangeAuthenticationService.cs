using OutlookSync.Domain.Services;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Infrastructure.Services.Exchange;

/// <summary>
/// Mock implementation of Exchange authentication service for testing purposes
/// </summary>
public class MockExchangeAuthenticationService : IExchangeAuthenticationService
{
    private const string MockToken = "mock-access-token-for-testing";
    
    /// <summary>
    /// Gets or sets whether authentication should fail (for testing)
    /// </summary>
    public bool ShouldFail { get; set; }
    
    /// <summary>
    /// Gets or sets the delay in milliseconds for authentication (for testing)
    /// </summary>
    public int DelayMs { get; set; }
    
    /// <summary>
    /// Gets the number of times authentication was called (for testing)
    /// </summary>
    public int CallCount { get; private set; }
    
    /// <inheritdoc/>
    public async Task<AccessToken> AuthenticateAsync(
        string clientId,
        string tenantId,
        string[] scopes,
        Func<string, string, Task> deviceCodeCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId, nameof(clientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        ArgumentNullException.ThrowIfNull(scopes, nameof(scopes));
        ArgumentNullException.ThrowIfNull(deviceCodeCallback, nameof(deviceCodeCallback));
        
        CallCount++;
        
        if (ShouldFail)
        {
            throw new InvalidOperationException("Mock authentication configured to fail");
        }
        
        if (DelayMs > 0)
        {
            await Task.Delay(DelayMs, cancellationToken);
        }
        
        // Simulate device code callback
        await deviceCodeCallback("https://microsoft.com/devicelogin", "MOCK1234");
        
        return new AccessToken
        {
            Token = MockToken,
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };
    }
    
    /// <summary>
    /// Resets the mock service state (for testing)
    /// </summary>
    public void Reset()
    {
        ShouldFail = false;
        DelayMs = 0;
        CallCount = 0;
    }
}
