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
            _bindings = await CalendarBindingRepository.Query.ToListAsync();
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
}
