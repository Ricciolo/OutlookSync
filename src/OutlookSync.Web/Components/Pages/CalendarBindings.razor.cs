using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Code-behind for CalendarBindings page component
/// </summary>
public partial class CalendarBindings
{
    private List<CalendarBinding>? _bindings;
    private bool _isLoading = true;
    private bool _isSyncing;
    private Guid? _syncingBindingId;
    private string? _syncMessage;
    private bool _syncSuccess;
    private AddCalendarBindingDialog? _addBindingDialog;
    private EditCalendarBindingDialog? _editBindingDialog;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await LoadBindingsAsync();
    }

    private async Task LoadBindingsAsync()
    {
        _isLoading = true;
        try
        {
            _bindings = await CalendarBindingRepository.Query.AsNoTracking().ToListAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task ShowAddBindingForm()
    {
        if (_addBindingDialog != null)
        {
            await _addBindingDialog.OpenAsync();
        }
    }

    private async Task ToggleBindingAsync(CalendarBinding binding)
    {
        if (binding.IsEnabled)
        {
            binding.Disable();
        }
        else
        {
            binding.Enable();
        }
        
        await CalendarBindingRepository.UpdateAsync(binding);
        await UnitOfWork.SaveChangesAsync();
        
        await LoadBindingsAsync();
    }

    private async Task DeleteBindingAsync(CalendarBinding binding)
    {
        await CalendarBindingRepository.DeleteAsync(binding);
        await UnitOfWork.SaveChangesAsync();
        
        await LoadBindingsAsync();
    }

    private async Task OnBindingAddedAsync()
    {
        await LoadBindingsAsync();
    }

    private async Task OnBindingUpdatedAsync()
    {
        await LoadBindingsAsync();
    }

    private async Task EditBinding(CalendarBinding binding)
    {
        if (_editBindingDialog != null)
        {
            await _editBindingDialog.OpenAsync(binding);
        }
    }

    private async Task SyncAllBindingsAsync()
    {
        if (SyncService.IsSyncing)
        {
            _syncMessage = "A synchronization is already in progress";
            _syncSuccess = false;
            return;
        }

        _isSyncing = true;
        _syncingBindingId = null;
        _syncMessage = null;
        StateHasChanged();

        try
        {
            var triggered = await SyncService.TriggerSyncAllAsync();
            
            if (triggered)
            {
                _syncMessage = "All calendar bindings synchronized successfully";
                _syncSuccess = true;
                await LoadBindingsAsync();
            }
            else
            {
                _syncMessage = "Cannot start synchronization: another sync is in progress";
                _syncSuccess = false;
            }
        }
        catch (Exception ex)
        {
            _syncMessage = $"Synchronization failed: {ex.Message}";
            _syncSuccess = false;
        }
        finally
        {
            _isSyncing = false;
            StateHasChanged();
        }
    }

    private async Task SyncBindingAsync(Guid bindingId)
    {
        if (SyncService.IsSyncing)
        {
            _syncMessage = "A synchronization is already in progress";
            _syncSuccess = false;
            return;
        }

        _isSyncing = true;
        _syncingBindingId = bindingId;
        _syncMessage = null;
        StateHasChanged();

        try
        {
            var triggered = await SyncService.TriggerSyncBindingAsync(bindingId);
            
            if (triggered)
            {
                var binding = _bindings?.FirstOrDefault(b => b.Id == bindingId);
                var bindingName = binding?.Name ?? "Binding";
                _syncMessage = $"{bindingName} synchronized successfully";
                _syncSuccess = true;
                await LoadBindingsAsync();
            }
            else
            {
                _syncMessage = "Cannot start synchronization: another sync is in progress";
                _syncSuccess = false;
            }
        }
        catch (Exception ex)
        {
            _syncMessage = $"Synchronization failed: {ex.Message}";
            _syncSuccess = false;
        }
        finally
        {
            _isSyncing = false;
            _syncingBindingId = null;
            StateHasChanged();
        }
    }

    private void ClearSyncMessage()
    {
        _syncMessage = null;
    }
}
