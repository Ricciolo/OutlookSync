using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.Services;

namespace OutlookSync.Web.Components.Modals;

/// <summary>
/// Modal component for device flow authentication setup
/// </summary>
public partial class DeviceFlowSetupModal : ComponentBase, IAsyncDisposable
{
    private DeviceFlowStep _currentStep = DeviceFlowStep.FriendlyName;
    private string _friendlyName = string.Empty;
    private string _errorMessage = string.Empty;
    private DeviceCodeInitiationResult? _deviceCodeResult;
    private Guid _currentSessionId;
    private System.Threading.Timer? _pollingTimer;
    private string _copyButtonText = "Copy Code";
    private bool _isProcessing;
    private IJSObjectReference? _clipboardModule;

    [Inject]
    private ICredentialsService CredentialsService { get; set; } = default!;

    [Inject]
    private ICredentialRepository CredentialRepository { get; set; } = default!;

    [Inject]
    private IUnitOfWork UnitOfWork { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private enum DeviceFlowStep
    {
        FriendlyName,
        WaitingForAuth,
        Success
    }

    /// <summary>
    /// Gets or sets whether the modal is visible
    /// </summary>
    [Parameter]
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the modal is cancelled
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the device flow is successfully completed
    /// </summary>
    [Parameter]
    public EventCallback OnComplete { get; set; }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _clipboardModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/clipboard.js");
        }
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        // Reset state when modal becomes visible
        if (IsVisible && _currentStep == DeviceFlowStep.Success)
        {
            ResetState();
        }
    }

    private void ResetState()
    {
        _friendlyName = string.Empty;
        _errorMessage = string.Empty;
        _currentStep = DeviceFlowStep.FriendlyName;
        _deviceCodeResult = null;
        _isProcessing = false;
        _copyButtonText = "Copy Code";
        StopPolling();
    }

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
            _currentStep = DeviceFlowStep.WaitingForAuth;

            StateHasChanged();

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
                    _currentStep = DeviceFlowStep.Success;
                    StateHasChanged();
                });
            }
            else if (!result.IsPending)
            {
                StopPolling();
                
                await InvokeAsync(() =>
                {
                    _errorMessage = result.ErrorMessage ?? "Authentication failed.";
                    _currentStep = DeviceFlowStep.FriendlyName;
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
                _currentStep = DeviceFlowStep.FriendlyName;
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
