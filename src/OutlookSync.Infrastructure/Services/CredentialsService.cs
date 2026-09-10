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
public partial class CredentialsService(ILogger<CredentialsService> logger) : ICredentialsService
{
    // Stores pending authentication sessions
    private readonly ConcurrentDictionary<Guid, PendingAuthSession> _pendingSessions = new();

    /// <inheritdoc/>
    public async Task<DeviceCodeInitiationResult> InitializeCredentialAsync(
        string friendlyName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(friendlyName, nameof(friendlyName));

        return await StartDeviceCodeFlowAsync(
            new Credential { FriendlyName = friendlyName },
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DeviceCodeInitiationResult> ReauthenticateCredentialAsync(
        Credential credential,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credential);

        return await StartDeviceCodeFlowAsync(credential, cancellationToken);
    }

    private async Task<DeviceCodeInitiationResult> StartDeviceCodeFlowAsync(
        Credential credential,
        CancellationToken cancellationToken)
    {

        // Cleanup expired sessions to prevent memory leaks
        CleanupExpiredSessions();

        LogInitiatingDeviceCodeFlow(logger, credential.FriendlyName);

        CancellationTokenSource? deviceCodeCts = null;

        try
        {
            // Create the public client application
            var app = MsalHelper.CreatePublicClientApplication();

            // Configure token cache BEFORE starting authentication
            // This ensures events are registered when MSAL serializes the token
            MsalHelper.ConfigureTokenCache(
                app,
                getStatusData: () => credential.StatusData,
                updateStatusData: data => credential.UpdateStatusData(data));

            // Create a cancellation token source for the device code flow
            deviceCodeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Use TaskCompletionSource to wait for device code generation
            var deviceCodeTcs = new TaskCompletionSource<DeviceCodeResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Start the device code flow (but don't await it yet)
            var authTask = app.AcquireTokenWithDeviceCode(
                MsalHelper.EwsScopes,
                result =>
                {
                    LogDeviceCodeGenerated(logger, result.UserCode, result.VerificationUrl.ToString());
                    
                    // Signal that device code is ready
                    deviceCodeTcs.TrySetResult(result);
                    
                    return Task.CompletedTask;
                })
                .ExecuteAsync(deviceCodeCts.Token);

            _ = PropagateDeviceCodeRequestFailureAsync(authTask, deviceCodeTcs);

            // Wait for the device code to be generated
            var deviceCodeResult = await deviceCodeTcs.Task.WaitAsync(cancellationToken);

            // Create a session to track this pending authentication
            var sessionId = Guid.NewGuid();
            var session = new PendingAuthSession
            {
                SessionId = sessionId,
                FriendlyName = credential.FriendlyName,
                Credential = credential,
                PublicClientApp = app,
                AuthenticationTask = authTask,
                CancellationTokenSource = deviceCodeCts,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresOn = deviceCodeResult.ExpiresOn
            };

            _pendingSessions[sessionId] = session;

            LogDeviceCodeFlowInitiated(logger, sessionId);

            return DeviceCodeInitiationResult.Success(
                sessionId,
                deviceCodeResult.UserCode,
                deviceCodeResult.VerificationUrl.ToString(),
                deviceCodeResult.Message,
                deviceCodeResult.ExpiresOn);
        }
        catch (MsalException ex)
        {
            deviceCodeCts?.Dispose();
            LogMsalInitiationFailed(logger, ex, credential.FriendlyName);
            return DeviceCodeInitiationResult.Failure($"Authentication initiation failed: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            deviceCodeCts?.Dispose();
            LogInitiationCancelled(logger, credential.FriendlyName);
            return DeviceCodeInitiationResult.Failure("Authentication initiation was cancelled");
        }
        catch (Exception ex)
        {
            deviceCodeCts?.Dispose();
            LogUnexpectedInitiationError(logger, ex, credential.FriendlyName);
            return DeviceCodeInitiationResult.Failure($"An unexpected error occurred: {ex.Message}");
        }
    }

    private static async Task PropagateDeviceCodeRequestFailureAsync(
        Task<AuthenticationResult> authenticationTask,
        TaskCompletionSource<DeviceCodeResult> deviceCodeTcs)
    {
        try
        {
            await authenticationTask.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            deviceCodeTcs.TrySetException(exception);
        }
    }

    /// <inheritdoc/>
    public async Task<CredentialCompletionResult> CompleteCredentialAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        // Cleanup expired sessions to prevent memory leaks
        CleanupExpiredSessions();

        LogAttemptingCompletion(logger, sessionId);

        if (!_pendingSessions.TryGetValue(sessionId, out var session))
        {
            LogSessionNotFound(logger, sessionId);
            return CredentialCompletionResult.Failure("Session not found or expired");
        }

        try
        {
            // Check if the session has expired
            if (DateTimeOffset.UtcNow > session.ExpiresOn)
            {
                LogSessionExpired(logger, sessionId);
                _pendingSessions.TryRemove(sessionId, out _);
                session.CancellationTokenSource.Cancel();
                session.CancellationTokenSource.Dispose();
                return CredentialCompletionResult.Failure("Device code has expired. Please start a new authentication");
            }

            // Check if authentication is complete
            if (!session.AuthenticationTask.IsCompleted)
            {
                LogAuthenticationPending(logger, sessionId);
                return CredentialCompletionResult.Pending();
            }

            // Get the authentication result
            var authResult = await session.AuthenticationTask;

            LogDeviceCodeFlowCompleted(logger, authResult.Account.Username);

            // Token cache was already configured in InitializeCredentialAsync
            // The credential's StatusData and TokenStatus should already be updated
            
            // Remove the session from pending sessions
            _pendingSessions.TryRemove(sessionId, out _);
            session.CancellationTokenSource.Dispose();

            return CredentialCompletionResult.Success(session.Credential);
        }
        catch (MsalException ex)
        {
            LogMsalCompletionFailed(logger, ex, sessionId);
            _pendingSessions.TryRemove(sessionId, out _);
            session.CancellationTokenSource.Cancel();
            session.CancellationTokenSource.Dispose();
            return CredentialCompletionResult.Failure($"Authentication failed: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            LogCompletionCancelled(logger, sessionId);
            _pendingSessions.TryRemove(sessionId, out _);
            session.CancellationTokenSource.Dispose();
            return CredentialCompletionResult.Failure("Authentication was cancelled");
        }
        catch (Exception ex)
        {
            LogUnexpectedCompletionError(logger, ex, sessionId);
            _pendingSessions.TryRemove(sessionId, out _);
            session.CancellationTokenSource.Cancel();
            session.CancellationTokenSource.Dispose();
            return CredentialCompletionResult.Failure($"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<CredentialCompletionResult> RefreshCredentialAsync(
        Credential credential,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credential);

        if (credential.StatusData is not { Length: > 0 })
        {
            return CredentialCompletionResult.Failure(
                "No token cache is available. Please use 'Sign in again' to renew this credential.");
        }

        try
        {
            var app = MsalHelper.CreatePublicClientApplication();
            MsalHelper.ConfigureTokenCache(
                app,
                getStatusData: () => credential.StatusData,
                updateStatusData: data => credential.UpdateStatusData(data));

            var account = (await app.GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault();
            if (account is null)
            {
                return CredentialCompletionResult.Failure(
                    "The refresh token is no longer available. Please use 'Sign in again' to renew this credential.");
            }

            await app.AcquireTokenSilent(MsalHelper.EwsScopes, account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return CredentialCompletionResult.Success(credential);
        }
        catch (MsalUiRequiredException)
        {
            return CredentialCompletionResult.Failure(
                "Microsoft requires you to sign in again. Please use 'Sign in again' to renew this credential.");
        }
        catch (MsalException ex)
        {
            LogMsalRefreshFailed(logger, ex, credential.FriendlyName);
            return CredentialCompletionResult.Failure($"Token refresh failed: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            return CredentialCompletionResult.Failure("Token refresh was cancelled.");
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
                
                LogSessionCleanedUp(logger, sessionId, session.ExpiresOn);
            }
        }

        if (expiredSessions.Count > 0)
        {
            LogSessionsCleanedUp(logger, expiredSessions.Count);
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
