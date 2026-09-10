using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Code-behind for Credentials page component
/// </summary>
public partial class Credentials
{
    private List<Credential>? _credentials;
    private bool _isLoading = true;
    private bool _showDeviceFlowSetup;
    private bool _showDeleteConfirmation;
    private Credential? _credentialToDelete;
    private Credential? _credentialToReauthenticate;
    private bool _showHelp;
    private Guid? _refreshingCredentialId;
    private string? _statusMessage;
    private bool _statusIsError;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await LoadCredentialsAsync();
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
        _credentialToReauthenticate = null;
        _showDeviceFlowSetup = true;
        _showHelp = false;
    }

    private void ReauthenticateCredential(Credential credential)
    {
        _credentialToReauthenticate = credential;
        _showDeviceFlowSetup = true;
        _showHelp = false;
        _statusMessage = null;
    }

    private void ToggleHelp()
    {
        _showHelp = !_showHelp;
    }

    private void CancelDeviceFlow()
    {
        _showDeviceFlowSetup = false;
        _credentialToReauthenticate = null;
    }

    private async Task CompleteDeviceFlowAsync()
    {
        _showDeviceFlowSetup = false;
        _credentialToReauthenticate = null;
        await LoadCredentialsAsync();
    }

    /// <summary>
    /// Refreshes the authentication token for a credential.
    /// </summary>
    private async Task RefreshCredentialAsync(Credential credential)
    {
        _refreshingCredentialId = credential.Id;
        _statusMessage = null;

        try
        {
            var result = await CredentialsService.RefreshCredentialAsync(credential);
            if (!result.IsSuccess)
            {
                _statusIsError = true;
                _statusMessage = result.ErrorMessage ?? "Token refresh failed.";
                return;
            }

            await UnitOfWork.SaveChangesAsync();
            _statusIsError = false;
            _statusMessage = "Token refreshed successfully.";
            await LoadCredentialsAsync();
        }
        catch (Exception ex)
        {
            _statusIsError = true;
            _statusMessage = $"Token refresh failed: {ex.Message}";
        }
        finally
        {
            _refreshingCredentialId = null;
        }
    }

    private void DeleteCredential(Credential credential)
    {
        _credentialToDelete = credential;
        _showDeleteConfirmation = true;
    }

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
}
