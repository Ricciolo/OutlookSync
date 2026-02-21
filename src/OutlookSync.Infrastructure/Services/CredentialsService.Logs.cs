using Microsoft.Extensions.Logging;

namespace OutlookSync.Infrastructure.Services;

public partial class CredentialsService
{
    [LoggerMessage(LogLevel.Information, "Initiating device code flow for credential: {FriendlyName}")]
    private static partial void LogInitiatingDeviceCodeFlow(ILogger logger, string friendlyName);

    [LoggerMessage(LogLevel.Information, "Device code generated. User code: {UserCode}, Verification URL: {VerificationUrl}")]
    private static partial void LogDeviceCodeGenerated(ILogger logger, string userCode, string verificationUrl);

    [LoggerMessage(LogLevel.Information, "Device code flow initiated successfully. Session ID: {SessionId}")]
    private static partial void LogDeviceCodeFlowInitiated(ILogger logger, Guid sessionId);

    [LoggerMessage(LogLevel.Error, "MSAL authentication initiation failed for credential: {FriendlyName}")]
    private static partial void LogMsalInitiationFailed(ILogger logger, Exception exception, string friendlyName);

    [LoggerMessage(LogLevel.Warning, "Credential initiation was cancelled for: {FriendlyName}")]
    private static partial void LogInitiationCancelled(ILogger logger, string friendlyName);

    [LoggerMessage(LogLevel.Error, "Unexpected error during credential initiation: {FriendlyName}")]
    private static partial void LogUnexpectedInitiationError(ILogger logger, Exception exception, string friendlyName);

    [LoggerMessage(LogLevel.Information, "Attempting to complete credential for session: {SessionId}")]
    private static partial void LogAttemptingCompletion(ILogger logger, Guid sessionId);

    [LoggerMessage(LogLevel.Warning, "Session not found: {SessionId}")]
    private static partial void LogSessionNotFound(ILogger logger, Guid sessionId);

    [LoggerMessage(LogLevel.Warning, "Session expired: {SessionId}")]
    private static partial void LogSessionExpired(ILogger logger, Guid sessionId);

    [LoggerMessage(LogLevel.Debug, "Authentication still pending for session: {SessionId}")]
    private static partial void LogAuthenticationPending(ILogger logger, Guid sessionId);

    [LoggerMessage(LogLevel.Information, "Device code flow completed successfully. Account: {Account}")]
    private static partial void LogDeviceCodeFlowCompleted(ILogger logger, string account);

    [LoggerMessage(LogLevel.Error, "MSAL authentication completion failed for session: {SessionId}")]
    private static partial void LogMsalCompletionFailed(ILogger logger, Exception exception, Guid sessionId);

    [LoggerMessage(LogLevel.Warning, "Credential completion was cancelled for session: {SessionId}")]
    private static partial void LogCompletionCancelled(ILogger logger, Guid sessionId);

    [LoggerMessage(LogLevel.Error, "Unexpected error during credential completion for session: {SessionId}")]
    private static partial void LogUnexpectedCompletionError(ILogger logger, Exception exception, Guid sessionId);

    [LoggerMessage(LogLevel.Debug, "Cleaned up expired session: {SessionId} (expired at {ExpiresOn})")]
    private static partial void LogSessionCleanedUp(ILogger logger, Guid sessionId, DateTimeOffset expiresOn);

    [LoggerMessage(LogLevel.Information, "Cleaned up {Count} expired authentication sessions")]
    private static partial void LogSessionsCleanedUp(ILogger logger, int count);
}
