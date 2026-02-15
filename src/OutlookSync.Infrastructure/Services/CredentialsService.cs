using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Services;
using OutlookSync.Infrastructure.Authentication;

namespace OutlookSync.Infrastructure.Services;

/// <summary>
/// Infrastructure service for managing credential initialization and authentication
/// </summary>
public class CredentialsService : ICredentialsService
{
    private readonly ILogger<CredentialsService> _logger;
    
    // Stores pending authentication sessions
    private readonly ConcurrentDictionary<Guid, PendingAuthSession> _pendingSessions = new();

    public CredentialsService(ILogger<CredentialsService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DeviceCodeInitiationResult> InitializeCredentialAsync(
        string friendlyName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(friendlyName, nameof(friendlyName));

        // Cleanup expired sessions to prevent memory leaks
        CleanupExpiredSessions();

        _logger.LogInformation("Initiating device code flow for credential: {FriendlyName}", friendlyName);

        try
        {
            // Create the public client application
            var app = MsalHelper.CreatePublicClientApplication();

            // Create a new credential entity
            var credential = new Credential
            {
                FriendlyName = friendlyName
            };

            // Create a cancellation token source for the device code flow
            var deviceCodeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Use TaskCompletionSource to wait for device code generation
            var deviceCodeTcs = new TaskCompletionSource<DeviceCodeResult>();

            // Start the device code flow (but don't await it yet)
            var authTask = app.AcquireTokenWithDeviceCode(
                MsalHelper.EwsScopes,
                result =>
                {
                    _logger.LogInformation(
                        "Device code generated. User code: {UserCode}, Verification URL: {VerificationUrl}",
                        result.UserCode,
                        result.VerificationUrl);
                    
                    // Signal that device code is ready
                    deviceCodeTcs.TrySetResult(result);
                    
                    return Task.CompletedTask;
                })
                .ExecuteAsync(deviceCodeCts.Token);

            // Wait for the device code to be generated
            var deviceCodeResult = await deviceCodeTcs.Task.WaitAsync(cancellationToken);

            // Create a session to track this pending authentication
            var sessionId = Guid.NewGuid();
            var session = new PendingAuthSession
            {
                SessionId = sessionId,
                FriendlyName = friendlyName,
                Credential = credential,
                PublicClientApp = app,
                AuthenticationTask = authTask,
                CancellationTokenSource = deviceCodeCts,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresOn = deviceCodeResult.ExpiresOn
            };

            _pendingSessions[sessionId] = session;

            _logger.LogInformation(
                "Device code flow initiated successfully. Session ID: {SessionId}",
                sessionId);

            return DeviceCodeInitiationResult.Success(
                sessionId,
                deviceCodeResult.UserCode,
                deviceCodeResult.VerificationUrl.ToString(),
                deviceCodeResult.Message,
                deviceCodeResult.ExpiresOn);
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "MSAL authentication initiation failed for credential: {FriendlyName}", friendlyName);
            return DeviceCodeInitiationResult.Failure($"Authentication initiation failed: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Credential initiation was cancelled for: {FriendlyName}", friendlyName);
            return DeviceCodeInitiationResult.Failure("Authentication initiation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during credential initiation: {FriendlyName}", friendlyName);
            return DeviceCodeInitiationResult.Failure($"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<CredentialCompletionResult> CompleteCredentialAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        // Cleanup expired sessions to prevent memory leaks
        CleanupExpiredSessions();

        _logger.LogInformation("Attempting to complete credential for session: {SessionId}", sessionId);

        if (!_pendingSessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return CredentialCompletionResult.Failure("Session not found or expired");
        }

        try
        {
            // Check if the session has expired
            if (DateTimeOffset.UtcNow > session.ExpiresOn)
            {
                _logger.LogWarning("Session expired: {SessionId}", sessionId);
                _pendingSessions.TryRemove(sessionId, out _);
                session.CancellationTokenSource.Cancel();
                session.CancellationTokenSource.Dispose();
                return CredentialCompletionResult.Failure("Device code has expired. Please start a new authentication");
            }

            // Check if authentication is complete
            if (!session.AuthenticationTask.IsCompleted)
            {
                _logger.LogDebug("Authentication still pending for session: {SessionId}", sessionId);
                return CredentialCompletionResult.Pending();
            }

            // Get the authentication result
            var authResult = await session.AuthenticationTask;

            _logger.LogInformation(
                "Device code flow completed successfully. Account: {Account}",
                authResult.Account.Username);

            // Configure token cache to update the credential
            MsalHelper.ConfigureTokenCache(
                session.PublicClientApp,
                getStatusData: () => session.Credential.StatusData,
                updateStatusData: data => session.Credential.UpdateStatusData(data));

            // Force token cache serialization
            await session.PublicClientApp.GetAccountsAsync();

            // Remove the session from pending sessions
            _pendingSessions.TryRemove(sessionId, out _);
            session.CancellationTokenSource.Dispose();

            return CredentialCompletionResult.Success(session.Credential);
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "MSAL authentication completion failed for session: {SessionId}", sessionId);
            _pendingSessions.TryRemove(sessionId, out _);
            session.CancellationTokenSource.Cancel();
            session.CancellationTokenSource.Dispose();
            return CredentialCompletionResult.Failure($"Authentication failed: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Credential completion was cancelled for session: {SessionId}", sessionId);
            _pendingSessions.TryRemove(sessionId, out _);
            session.CancellationTokenSource.Dispose();
            return CredentialCompletionResult.Failure("Authentication was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during credential completion for session: {SessionId}", sessionId);
            _pendingSessions.TryRemove(sessionId, out _);
            session.CancellationTokenSource.Cancel();
            session.CancellationTokenSource.Dispose();
            return CredentialCompletionResult.Failure($"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans up expired authentication sessions to prevent memory leaks
    /// </summary>
    private void CleanupExpiredSessions()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredSessions = _pendingSessions
            .Where(kvp => now > kvp.Value.ExpiresOn)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in expiredSessions)
        {
            if (_pendingSessions.TryRemove(sessionId, out var session))
            {
                // Cancel the authentication task
                session.CancellationTokenSource.Cancel();
                session.CancellationTokenSource.Dispose();
                
                _logger.LogDebug(
                    "Cleaned up expired session: {SessionId} (expired at {ExpiresOn})",
                    sessionId,
                    session.ExpiresOn);
            }
        }

        if (expiredSessions.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired authentication sessions", expiredSessions.Count);
        }
    }

    /// <summary>
    /// Represents a pending authentication session
    /// </summary>
    private sealed class PendingAuthSession
    {
        public required Guid SessionId { get; init; }
        public required string FriendlyName { get; init; }
        public required Credential Credential { get; init; }
        public required IPublicClientApplication PublicClientApp { get; init; }
        public required Task<AuthenticationResult> AuthenticationTask { get; init; }
        public required CancellationTokenSource CancellationTokenSource { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required DateTimeOffset ExpiresOn { get; init; }
    }
}
