using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Services;
using DomainAccessToken = OutlookSync.Domain.ValueObjects.AccessToken;

namespace OutlookSync.Infrastructure.Services.Exchange;

/// <summary>
/// Implementation of Exchange authentication using Azure.Identity Device Flow
/// </summary>
public class ExchangeAuthenticationService(ILogger<ExchangeAuthenticationService> logger) 
    : IExchangeAuthenticationService
{
    /// <inheritdoc/>
    public async Task<DomainAccessToken> AuthenticateAsync(
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
        
        if (scopes.Length == 0)
        {
            throw new ArgumentException("At least one scope is required", nameof(scopes));
        }
        
        logger.LogInformation("Starting Device Flow authentication for client {ClientId}", clientId);
        throw new NotImplementedException();
    }
}
