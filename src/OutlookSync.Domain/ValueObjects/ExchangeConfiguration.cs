namespace OutlookSync.Domain.ValueObjects;

/// <summary>
/// Configuration for Exchange service connection
/// </summary>
public record ExchangeConfiguration
{
    /// <summary>
    /// Client ID for OAuth authentication (e.g., Microsoft Office client ID)
    /// </summary>
    public required string ClientId { get; init; }
    
    /// <summary>
    /// Tenant ID for authentication (use "common" for multi-tenant)
    /// </summary>
    public required string TenantId { get; init; }
    
    /// <summary>
    /// Exchange Web Services URL
    /// </summary>
    public required string ServiceUrl { get; init; }
    
    /// <summary>
    /// OAuth scopes for EWS access
    /// </summary>
    public required string[] Scopes { get; init; }
    
    /// <summary>
    /// Timeout in seconds for Exchange service operations
    /// </summary>
    public int TimeoutSeconds { get; init; } = 100;
    
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;
    
    /// <summary>
    /// Initial retry delay in milliseconds
    /// </summary>
    public int InitialRetryDelayMs { get; init; } = 1000;
    
    /// <summary>
    /// Creates default configuration for Microsoft Office 365
    /// </summary>
    public static ExchangeConfiguration CreateDefault() => new()
    {
        ClientId = "d3590ed6-52b3-4102-aeff-aad2292ab01c", // Microsoft Office client ID
        TenantId = "common",
        ServiceUrl = "https://outlook.office365.com/EWS/Exchange.asmx",
        Scopes = ["https://outlook.office365.com/EWS.AccessAsUser.All"],
        TimeoutSeconds = 100,
        MaxRetryAttempts = 3,
        InitialRetryDelayMs = 1000
    };
}
