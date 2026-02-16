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
    private bool _showHelp;

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
        _showDeviceFlowSetup = true;
        _showHelp = false;
    }

    private void ToggleHelp()
    {
        _showHelp = !_showHelp;
    }

    private void CancelDeviceFlow()
    {
        _showDeviceFlowSetup = false;
    }

    private async Task CompleteDeviceFlowAsync()
    {
        _showDeviceFlowSetup = false;
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
