using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Domain.Services;

/// <summary>
/// Domain service for managing credential initialization and authentication using device code flow
/// </summary>
public interface ICredentialsService
{
    /// <summary>
    /// Initiates device code flow authentication and returns device code information
    /// </summary>
    /// <param name="friendlyName">Friendly name for the credential</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing device code information for user authentication</returns>
    Task<DeviceCodeInitiationResult> InitializeCredentialAsync(
        string friendlyName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Completes the device code flow authentication and returns the initialized credential
    /// </summary>
    /// <param name="sessionId">The session ID returned from InitializeCredentialAsync</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the initialized credential or error information</returns>
    Task<CredentialCompletionResult> CompleteCredentialAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of device code flow initiation
/// </summary>
public record DeviceCodeInitiationResult
{
    /// <summary>
    /// Gets the session ID to use for completing the authentication
    /// </summary>
    public required Guid SessionId { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether the initiation was successful
    /// </summary>
    public required bool IsSuccess { get; init; }
    
    /// <summary>
    /// Gets the error message if initiation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Gets the user code to be entered at the verification URL
    /// </summary>
    public string? UserCode { get; init; }
    
    /// <summary>
    /// Gets the verification URL where the user should authenticate
    /// </summary>
    public string? VerificationUrl { get; init; }
    
    /// <summary>
    /// Gets the complete message to display to the user
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// Gets the expiration time for the device code
    /// </summary>
    public DateTimeOffset? ExpiresOn { get; init; }
    
    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static DeviceCodeInitiationResult Success(
        Guid sessionId,
        string userCode,
        string verificationUrl,
        string message,
        DateTimeOffset expiresOn) =>
        new()
        {
            SessionId = sessionId,
            IsSuccess = true,
            UserCode = userCode,
            VerificationUrl = verificationUrl,
            Message = message,
            ExpiresOn = expiresOn
        };
    
    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static DeviceCodeInitiationResult Failure(string errorMessage) =>
        new()
        {
            SessionId = Guid.Empty,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Result of credential completion
/// </summary>
public record CredentialCompletionResult
{
    /// <summary>
    /// Gets the initialized credential if successful
    /// </summary>
    public Credential? Credential { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether the completion was successful
    /// </summary>
    public required bool IsSuccess { get; init; }
    
    /// <summary>
    /// Gets the error message if completion failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether the user is still pending authentication
    /// </summary>
    public bool IsPending { get; init; }
    
    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static CredentialCompletionResult Success(Credential credential) =>
        new()
        {
            IsSuccess = true,
            Credential = credential
        };
    
    /// <summary>
    /// Creates a pending result (user hasn't completed authentication yet)
    /// </summary>
    public static CredentialCompletionResult Pending() =>
        new()
        {
            IsSuccess = false,
            IsPending = true,
            ErrorMessage = "User authentication is still pending"
        };
    
    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static CredentialCompletionResult Failure(string errorMessage) =>
        new()
        {
            IsSuccess = false,
            IsPending = false,
            ErrorMessage = errorMessage
        };
}
