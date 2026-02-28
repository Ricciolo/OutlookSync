using Microsoft.EntityFrameworkCore;
using OutlookSync.Application.Services;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Code-behind for CalendarBindings page component
/// </summary>
public partial class CalendarBindings : IDisposable
{
    private List<CalendarBinding>? _bindings;
    private IReadOnlyList<ScheduledBindingInfo>? _scheduledBindings;
    private bool _isLoading = true;
    private bool _isSyncing;
    private Guid? _syncingBindingId;
    private string? _syncMessage;
    private bool _syncSuccess;
    private AddCalendarBindingDialog? _addBindingDialog;
    private EditCalendarBindingDialog? _editBindingDialog;
    private System.Timers.Timer? _countdownTimer;
    private System.Timers.Timer? _scheduledInfoTimer;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await LoadBindingsAsync();
        await LoadScheduledBindingsAsync();
        
        // Refresh the countdown display every minute
        _countdownTimer = new System.Timers.Timer(60_000);
        _countdownTimer.Elapsed += async (sender, e) => await InvokeAsync(StateHasChanged);
        _countdownTimer.AutoReset = true;
        _countdownTimer.Start();
        
        // Reload scheduled bindings info from service every 5 minutes
        _scheduledInfoTimer = new System.Timers.Timer(300_000);
        _scheduledInfoTimer.Elapsed += async (sender, e) => await RefreshScheduledBindingsAsync();
        _scheduledInfoTimer.AutoReset = true;
        _scheduledInfoTimer.Start();
    }

    private async Task LoadBindingsAsync()
    {
        _isLoading = true;
        try
        {
            _bindings = await CalendarBindingRepository.Query.ToListAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadScheduledBindingsAsync()
    {
        try
        {
            _scheduledBindings = await SyncService.GetScheduledBindingsAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't fail the page load
            Console.WriteLine($"Error loading scheduled bindings: {ex.Message}");
        }
    }

    private async Task RefreshScheduledBindingsAsync()
    {
        await LoadScheduledBindingsAsync();
        await InvokeAsync(StateHasChanged);
    }

    private ScheduledBindingInfo? GetScheduledInfo(Guid bindingId)
    {
        return _scheduledBindings?.FirstOrDefault(s => s.BindingId == bindingId);
    }

    private static string GetTimeUntilNextSync(DateTime nextSyncAt)
    {
        var timeUntil = nextSyncAt - DateTime.UtcNow;
        
        if (timeUntil.TotalSeconds < 0)
        {
            return "syncing soon...";
        }
        else if (timeUntil.TotalMinutes < 1)
        {
            return $"in {(int)timeUntil.TotalSeconds}s";
        }
        else if (timeUntil.TotalHours < 1)
        {
            return $"in {(int)timeUntil.TotalMinutes}m";
        }
        else if (timeUntil.TotalDays < 1)
        {
            return $"in {(int)timeUntil.TotalHours}h {(int)(timeUntil.TotalMinutes % 60)}m";
        }
        else
        {
            return $"in {(int)timeUntil.TotalDays}d";
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
        await SyncService.RescheduleAllAsync();
    }

    private async Task DeleteBindingAsync(CalendarBinding binding)
    {
        await CalendarBindingRepository.DeleteAsync(binding);
        await UnitOfWork.SaveChangesAsync();
        
        await LoadBindingsAsync();
        await SyncService.RescheduleAllAsync();
    }

    private async Task OnBindingAddedAsync()
    {
        await LoadBindingsAsync();
        await LoadScheduledBindingsAsync();
        await SyncService.RescheduleAllAsync();
    }

    private async Task OnBindingUpdatedAsync()
    {
        await LoadBindingsAsync();
        await SyncService.RescheduleAllAsync();
        await LoadScheduledBindingsAsync();
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
                await LoadScheduledBindingsAsync();
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
                await LoadScheduledBindingsAsync();
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_countdownTimer != null)
        {
            _countdownTimer.Stop();
            _countdownTimer.Dispose();
        }

        if (_scheduledInfoTimer != null)
        {
            _scheduledInfoTimer.Stop();
            _scheduledInfoTimer.Dispose();
        }
    }
}
