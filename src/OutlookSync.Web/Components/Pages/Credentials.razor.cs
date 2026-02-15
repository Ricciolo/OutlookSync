using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Services;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Code-behind for Credentials page component
/// </summary>
public partial class Credentials : IAsyncDisposable
{
    private List<Credential>? _credentials;
    private bool _isLoading = true;
    private bool _showDeviceFlowSetup;
    private bool _isProcessing;
    private DeviceFlowStep _deviceFlowStep = DeviceFlowStep.FriendlyName;
    private string _friendlyName = string.Empty;
    private string _errorMessage = string.Empty;
    private DeviceCodeInitiationResult? _deviceCodeResult;
    private Guid _currentSessionId;
    private System.Threading.Timer? _pollingTimer;
    private string _copyButtonText = "Copy Code";
    private bool _showDeleteConfirmation;
    private Credential? _credentialToDelete;
    private bool _showHelp;
    private IJSObjectReference? _clipboardModule;

    private enum DeviceFlowStep
    {
        FriendlyName,
        WaitingForAuth,
        Success
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await LoadCredentialsAsync();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _clipboardModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/clipboard.js");
        }
    }

    private async Task LoadCredentialsAsync()
    {
        _isLoading = true;
        try
        {
            _credentials = await CredentialRepository.Query.ToListAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void InitiateDeviceFlow()
    {
        _friendlyName = string.Empty;
        _errorMessage = string.Empty;
        _deviceFlowStep = DeviceFlowStep.FriendlyName;
        _showDeviceFlowSetup = true;
        _showHelp = false;
    }

    private void ToggleHelp()
    {
        _showHelp = !_showHelp;
    }

    /// <summary>
    /// Starts the device code flow for authentication (synchronous wrapper)
    /// </summary>
    private void StartDeviceFlow() => _ = StartDeviceFlowAsync();

    /// <summary>
    /// Starts the device code flow for authentication
    /// </summary>
    private async Task StartDeviceFlowAsync()
    {
        if (string.IsNullOrWhiteSpace(_friendlyName))
        {
            _errorMessage = "Please enter a friendly name for this credential.";
            return;
        }

        _isProcessing = true;
        _errorMessage = string.Empty;

        try
        {
            var result = await CredentialsService.InitializeCredentialAsync(_friendlyName);

            if (!result.IsSuccess)
            {
                _errorMessage = result.ErrorMessage ?? "Failed to initialize authentication.";
                return;
            }

            _deviceCodeResult = result;
            _currentSessionId = result.SessionId;
            _deviceFlowStep = DeviceFlowStep.WaitingForAuth;

            StartPolling();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void StartPolling()
    {
        _pollingTimer = new System.Threading.Timer(
            async _ => await PollForCompletionAsync(),
            null,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(3));
    }

    private async Task PollForCompletionAsync()
    {
        try
        {
            var result = await CredentialsService.CompleteCredentialAsync(_currentSessionId);

            if (result.IsSuccess && result.Credential != null)
            {
                StopPolling();
                
                await CredentialRepository.AddAsync(result.Credential);
                await UnitOfWork.SaveChangesAsync();

                await InvokeAsync(() =>
                {
                    _deviceFlowStep = DeviceFlowStep.Success;
                    StateHasChanged();
                });
            }
            else if (!result.IsPending)
            {
                StopPolling();
                
                await InvokeAsync(() =>
                {
                    _errorMessage = result.ErrorMessage ?? "Authentication failed.";
                    _deviceFlowStep = DeviceFlowStep.FriendlyName;
                    StateHasChanged();
                });
            }
        }
        catch (Exception ex)
        {
            StopPolling();
            
            await InvokeAsync(() =>
            {
                _errorMessage = $"Error during authentication: {ex.Message}";
                _deviceFlowStep = DeviceFlowStep.FriendlyName;
                StateHasChanged();
            });
        }
    }

    private void StopPolling()
    {
        _pollingTimer?.Dispose();
        _pollingTimer = null;
    }

    private async Task CopyCodeToClipboardAsync()
    {
        if (_deviceCodeResult?.UserCode != null && _clipboardModule != null)
        {
            try
            {
                var success = await _clipboardModule.InvokeAsync<bool>("copyToClipboard", _deviceCodeResult.UserCode);
                
                if (success)
                {
                    _copyButtonText = "Copied!";
                    await Task.Delay(2000);
                    _copyButtonText = "Copy Code";
                    StateHasChanged();
                }
            }
            catch (Exception)
            {
                _copyButtonText = "Copy Failed";
                await Task.Delay(2000);
                _copyButtonText = "Copy Code";
                StateHasChanged();
            }
        }
    }

    private void CancelDeviceFlow()
    {
        StopPolling();
        _showDeviceFlowSetup = false;
        _deviceFlowStep = DeviceFlowStep.FriendlyName;
        _friendlyName = string.Empty;
        _errorMessage = string.Empty;
        _deviceCodeResult = null;
    }

    /// <summary>
    /// Completes the device flow and refreshes the credentials list (synchronous wrapper)
    /// </summary>
    private void CompleteDeviceFlow() => _ = CompleteDeviceFlowAsync();

    /// <summary>
    /// Completes the device flow and refreshes the credentials list
    /// </summary>
    private async Task CompleteDeviceFlowAsync()
    {
        _showDeviceFlowSetup = false;
        _deviceFlowStep = DeviceFlowStep.FriendlyName;
        _friendlyName = string.Empty;
        _errorMessage = string.Empty;
        _deviceCodeResult = null;
        await LoadCredentialsAsync();
    }

    /// <summary>
    /// Refreshes the authentication token for a credential (synchronous wrapper)
    /// </summary>
    private void RefreshCredential(Credential credential) => _ = RefreshCredentialAsync(credential);

    /// <summary>
    /// Refreshes the authentication token for a credential
    /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter - Parameter will be used when implemented
    private async Task RefreshCredentialAsync(Credential credential)
    {
        // TODO: Implement token refresh logic using the existing credential
        await LoadCredentialsAsync();
    }
#pragma warning restore IDE0060

    private void DeleteCredential(Credential credential)
    {
        _credentialToDelete = credential;
        _showDeleteConfirmation = true;
    }

    /// <summary>
    /// Confirms the deletion of a credential (synchronous wrapper)
    /// </summary>
    private void ConfirmDelete() => _ = ConfirmDeleteAsync();

    /// <summary>
    /// Confirms the deletion of a credential
    /// </summary>
    private async Task ConfirmDeleteAsync()
    {
        if (_credentialToDelete != null)
        {
            await CredentialRepository.DeleteAsync(_credentialToDelete);
            await UnitOfWork.SaveChangesAsync();
            await LoadCredentialsAsync();
        }
        
        _showDeleteConfirmation = false;
        _credentialToDelete = null;
    }

    private void CancelDelete()
    {
        _showDeleteConfirmation = false;
        _credentialToDelete = null;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        StopPolling();
        
        if (_clipboardModule != null)
        {
            await _clipboardModule.DisposeAsync();
        }
    }
}
