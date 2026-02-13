using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Services;

/// <summary>
/// Service for Exchange authentication using Device Flow OAuth
/// </summary>
public interface IExchangeAuthenticationService
{
    /// <summary>
    /// Authenticates using Device Flow OAuth and returns an access token
    /// </summary>
    /// <param name="clientId">OAuth client ID</param>
    /// <param name="tenantId">Tenant ID or "common"</param>
    /// <param name="scopes">OAuth scopes to request</param>
    /// <param name="deviceCodeCallback">Callback for displaying device code to user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access token for EWS</returns>
    Task<AccessToken> AuthenticateAsync(
        string clientId,
        string tenantId,
        string[] scopes,
        Func<string, string, Task> deviceCodeCallback,
        CancellationToken cancellationToken = default);
}
