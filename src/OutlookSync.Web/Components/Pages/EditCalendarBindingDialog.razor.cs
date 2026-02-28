using Microsoft.AspNetCore.Components;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Dialog component for editing an existing calendar binding
/// </summary>
public partial class EditCalendarBindingDialog
{
    private bool _isDialogOpen;
    private CalendarBinding? _binding;
    private string _bindingName = string.Empty;
    private CalendarBindingSettingsForm? _settingsForm;
    private bool _isSaving;
    private string? _errorMessage;
    private string? _successMessage;
    
    // Names for display
    private string? _sourceCredentialName;
    private string? _sourceCalendarName;
    private string? _targetCredentialName;
    private string? _targetCalendarName;
    private bool _isLoadingNames;

    [Parameter]
    public EventCallback OnBindingUpdated { get; set; }

    /// <summary>
    /// Opens the dialog with an existing binding
    /// </summary>
    public async Task OpenAsync(CalendarBinding binding)
    {
        _binding = binding;
        _bindingName = binding.Name;
        _errorMessage = null;
        _successMessage = null;
        
        _isDialogOpen = true;
        StateHasChanged();
        
        // Load credential and calendar names
        await LoadNamesAsync();
        
        // Wait for the settings form to be initialized
        await Task.Delay(100);
        
        if (_settingsForm != null)
        {
            _settingsForm.SetConfiguration(binding.Configuration);
        }
    }

    /// <summary>
    /// Loads the names of credentials and calendars for display
    /// </summary>
    private async Task LoadNamesAsync()
    {
        if (_binding == null)
        {
            return;
        }

        _isLoadingNames = true;
        StateHasChanged();

        try
        {
            // Load source credential
            var sourceCredential = await CredentialRepository.GetByIdAsync(_binding.SourceCredentialId);
            _sourceCredentialName = sourceCredential?.FriendlyName ?? "Unknown Credential";

            // Load target credential
            var targetCredential = await CredentialRepository.GetByIdAsync(_binding.TargetCredentialId);
            _targetCredentialName = targetCredential?.FriendlyName ?? "Unknown Credential";

            // Load source calendar name
            if (sourceCredential != null)
            {
                var sourceRepo = CalendarEventRepositoryFactory.Create(sourceCredential);
                await sourceRepo.InitAsync();
                var sourceCalendar = await sourceRepo.GetAvailableCalendarByIdAsync(_binding.SourceCalendarExternalId);
                _sourceCalendarName = sourceCalendar?.Name ?? "Unknown Calendar";
            }
            else
            {
                _sourceCalendarName = "Unknown Calendar";
            }

            // Load target calendar name
            if (targetCredential != null)
            {
                var targetRepo = CalendarEventRepositoryFactory.Create(targetCredential);
                await targetRepo.InitAsync();
                var targetCalendar = await targetRepo.GetAvailableCalendarByIdAsync(_binding.TargetCalendarExternalId);
                _targetCalendarName = targetCalendar?.Name ?? "Unknown Calendar";
            }
            else
            {
                _targetCalendarName = "Unknown Calendar";
            }
        }
        catch (Exception ex)
        {
            LogFailedToLoadNames(Logger, ex, _binding.Id);
            _sourceCredentialName = "Error loading name";
            _sourceCalendarName = "Error loading name";
            _targetCredentialName = "Error loading name";
            _targetCalendarName = "Error loading name";
        }
        finally
        {
            _isLoadingNames = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Closes the dialog
    /// </summary>
    private void CloseDialog()
    {
        _isDialogOpen = false;
        _binding = null;
        _bindingName = string.Empty;
        _errorMessage = null;
        _successMessage = null;
        _sourceCredentialName = null;
        _sourceCalendarName = null;
        _targetCredentialName = null;
        _targetCalendarName = null;
        StateHasChanged();
    }

    /// <summary>
    /// Saves the changes to the binding
    /// </summary>
    private async Task SaveChanges()
    {
        if (_binding == null || string.IsNullOrWhiteSpace(_bindingName) || _settingsForm == null)
        {
            return;
        }

        _isSaving = true;
        _errorMessage = null;
        _successMessage = null;

        try
        {
            // Update name if changed
            if (_binding.Name != _bindingName.Trim())
            {
                _binding.Rename(_bindingName.Trim());
            }

            // Update configuration
            var configuration = _settingsForm.GetConfiguration();
            _binding.UpdateConfiguration(configuration);

            await CalendarBindingRepository.UpdateAsync(_binding);
            await UnitOfWork.SaveChangesAsync();

            LogBindingUpdated(Logger, _binding.Id);

            _successMessage = "Binding updated successfully!";
            StateHasChanged();

            // Wait a moment to show success message
            await Task.Delay(1000);

            CloseDialog();
            
            await OnBindingUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            LogFailedToUpdateBinding(Logger, ex, _binding.Id);
            _errorMessage = $"Failed to update binding: {ex.Message}";
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }
}
