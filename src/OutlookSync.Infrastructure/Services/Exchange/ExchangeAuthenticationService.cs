using Azure.Core;
using Azure.Identity;
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
        
        var options = new DeviceCodeCredentialOptions
        {
            ClientId = clientId,
            TenantId = tenantId,
            DeviceCodeCallback = async (code, ct) =>
            {
                logger.LogInformation(
                    "Device code authentication required. Verification URI: {Uri}, Code: {Code}",
                    code.VerificationUri,
                    code.UserCode);
                
                await deviceCodeCallback(code.VerificationUri.ToString(), code.UserCode);
            }
        };
        
        var credential = new DeviceCodeCredential(options);
        
        try
        {
            logger.LogInformation("Waiting for user authentication...");
            
            var context = new TokenRequestContext(scopes);
            var azureToken = await credential.GetTokenAsync(context, cancellationToken);
            
            logger.LogInformation("Authentication successful. Token expires at {ExpiresOn}", azureToken.ExpiresOn);
            
            return new DomainAccessToken
            {
                Token = azureToken.Token,
                ExpiresOn = azureToken.ExpiresOn
            };
        }
        catch (AuthenticationFailedException ex)
        {
            logger.LogError(ex, "Authentication failed: {Message}", ex.Message);
            throw new InvalidOperationException(
                "Failed to authenticate with Exchange. " +
                "This may be due to incorrect credentials, disabled account, or admin restrictions.", 
                ex);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Authentication was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during authentication: {Message}", ex.Message);
            throw;
        }
    }
}
