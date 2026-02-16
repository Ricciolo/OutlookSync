using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Code-behind for Calendars page component
/// </summary>
public partial class Calendars
{
    private List<Calendar>? _calendars;
    private bool _isLoading = true;
    private AddCalendarDialog? _addCalendarDialog;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await LoadCalendarsAsync();
    }

    private async Task LoadCalendarsAsync()
    {
        _isLoading = true;
        try
        {
            _calendars = await CalendarRepository.Query.ToListAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task ShowAddCalendarForm()
    {
        if (_addCalendarDialog != null)
        {
            await _addCalendarDialog.OpenAsync();
        }
    }

    private async Task ToggleCalendarAsync(Calendar calendar)
    {
        if (calendar.IsEnabled)
        {
            calendar.Disable();
        }
        else
        {
            calendar.Enable();
        }
        
        await CalendarRepository.UpdateAsync(calendar);
        await LoadCalendarsAsync();
    }

    private async Task OnCalendarAddedAsync()
    {
        await LoadCalendarsAsync();
    }

#pragma warning disable CA1822 // Mark members as static - Will be implemented with instance access
#pragma warning disable IDE0060 // Remove unused parameter - Parameter will be used when implemented
    private void EditCalendar(Calendar calendar)
    {
        // TODO: Navigate to edit calendar page or show modal
    }
#pragma warning restore IDE0060
#pragma warning restore CA1822
}
