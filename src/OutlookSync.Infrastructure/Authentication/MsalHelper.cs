using Microsoft.Identity.Client;

namespace OutlookSync.Infrastructure.Authentication;

/// <summary>
/// Helper class for centralized MSAL (Microsoft Authentication Library) operations
/// </summary>
public static class MsalHelper
{
    /// <summary>
    /// Office 365 Client ID for public client applications
    /// </summary>
    public const string OfficeClientId = "d3590ed6-52b3-4102-aeff-aad2292ab01c";
    
    /// <summary>
    /// EWS (Exchange Web Services) scope for accessing user calendars
    /// </summary>
    private const string EwsScope = "https://outlook.office365.com/EWS.AccessAsUser.All";
    
    /// <summary>
    /// Gets the required scopes for EWS calendar access
    /// </summary>
    public static string[] EwsScopes => [EwsScope];
    
    /// <summary>
    /// Creates a configured Public Client Application for interactive authentication
    /// </summary>
    /// <returns>A configured IPublicClientApplication instance</returns>
    public static IPublicClientApplication CreatePublicClientApplication()
    {
        return PublicClientApplicationBuilder
            .Create(OfficeClientId)
            .WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
            .Build();
    }
    
    /// <summary>
    /// Configures token cache serialization for a credential
    /// </summary>
    /// <param name="app">The public client application</param>
    /// <param name="getStatusData">Function to get the current status data</param>
    /// <param name="updateStatusData">Action to update the status data</param>
    public static void ConfigureTokenCache(
        IPublicClientApplication app,
        Func<byte[]?> getStatusData,
        Action<byte[]> updateStatusData)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        ArgumentNullException.ThrowIfNull(getStatusData, nameof(getStatusData));
        ArgumentNullException.ThrowIfNull(updateStatusData, nameof(updateStatusData));
        
        app.UserTokenCache.SetBeforeAccess(args =>
        {
            var statusData = getStatusData();
            if (statusData != null && statusData.Length > 0)
            {
                args.TokenCache.DeserializeMsalV3(statusData);
            }
        });

        app.UserTokenCache.SetAfterAccess(args =>
        {
            if (args.HasStateChanged)
            {
                var tokenCacheData = args.TokenCache.SerializeMsalV3();
                updateStatusData(tokenCacheData);
            }
        });
    }
}
