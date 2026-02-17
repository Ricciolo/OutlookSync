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
    private EditCalendarDialog? _editCalendarDialog;

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
        await UnitOfWork.SaveChangesAsync();
        
        await LoadCalendarsAsync();
    }

    private async Task OnCalendarAddedAsync()
    {
        await LoadCalendarsAsync();
    }

    private async Task OnCalendarUpdatedAsync()
    {
        await LoadCalendarsAsync();
    }

    private async Task EditCalendar(Calendar calendar)
    {
        if (_editCalendarDialog != null)
        {
            await _editCalendarDialog.OpenAsync(calendar);
        }
    }
}
