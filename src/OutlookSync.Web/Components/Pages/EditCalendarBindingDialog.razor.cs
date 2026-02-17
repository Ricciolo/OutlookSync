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
        
        // Wait for the settings form to be initialized
        await Task.Delay(100);
        
        if (_settingsForm != null)
        {
            _settingsForm.SetConfiguration(binding.Configuration);
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

            Logger.LogInformation("Calendar binding {BindingId} updated successfully", _binding.Id);

            _successMessage = "Binding updated successfully!";
            StateHasChanged();

            // Wait a moment to show success message
            await Task.Delay(1000);

            CloseDialog();
            
            await OnBindingUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update calendar binding {BindingId}", _binding.Id);
            _errorMessage = $"Failed to update binding: {ex.Message}";
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }
}
