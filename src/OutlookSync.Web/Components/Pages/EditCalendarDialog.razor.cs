using Microsoft.AspNetCore.Components;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Web.Components.Shared;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Dialog component for editing calendar settings
/// </summary>
public partial class EditCalendarDialog
{
    private bool _isDialogOpen;
    private Calendar? _calendar;
    private string? _errorMessage;
    private bool _isSaving;
    private CalendarSettingsModel _settings = new();

    /// <summary>
    /// Event callback triggered when calendar is updated
    /// </summary>
    [Parameter]
    public EventCallback OnCalendarUpdated { get; set; }

    /// <summary>
    /// Opens the dialog with the specified calendar
    /// </summary>
    /// <param name="calendar">The calendar to edit</param>
    public async Task OpenAsync(Calendar calendar)
    {
        _calendar = calendar;
        _isDialogOpen = true;
        _errorMessage = null;
        
        // Load current settings
        _settings = new CalendarSettingsModel
        {
            CalendarName = calendar.Name,
            SelectedInterval = calendar.Configuration.Interval.Minutes,
            StartDate = calendar.Configuration.StartDate,
            SyncDaysForward = calendar.Configuration.SyncDaysForward,
            IsPrivate = calendar.Configuration.IsPrivate,
            FieldSelectionType = IsEssentialFieldSelection(calendar.Configuration.FieldSelection) ? "essential" : "all"
        };
        
        StateHasChanged();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Closes the dialog and resets state
    /// </summary>
    private void CloseDialog()
    {
        _isDialogOpen = false;
        _calendar = null;
        _errorMessage = null;
        ResetForm();
        StateHasChanged();
    }

    /// <summary>
    /// Resets the form to default values
    /// </summary>
    private void ResetForm()
    {
        _settings = new CalendarSettingsModel();
    }

    /// <summary>
    /// Saves the changes to the database
    /// </summary>
    private async Task SaveChangesAsync()
    {
        if (_calendar == null || string.IsNullOrWhiteSpace(_settings.CalendarName))
        {
            return;
        }

        _isSaving = true;
        _errorMessage = null;
        
        try
        {
            var syncInterval = _settings.SelectedInterval switch
            {
                15 => SyncInterval.Every15Minutes(),
                30 => SyncInterval.Every30Minutes(),
                60 => SyncInterval.Hourly(),
                _ => SyncInterval.Custom(_settings.SelectedInterval)
            };

            var fieldSelection = _settings.FieldSelectionType == "all" 
                ? CalendarFieldSelection.All() 
                : CalendarFieldSelection.Essential();

            // Update calendar properties
            _calendar.Rename(_settings.CalendarName);
            _calendar.UpdateConfiguration(new SyncConfiguration
            {
                Interval = syncInterval,
                StartDate = _settings.StartDate,
                SyncDaysForward = _settings.SyncDaysForward,
                IsPrivate = _settings.IsPrivate,
                FieldSelection = fieldSelection
            });

            await CalendarRepository.UpdateAsync(_calendar);
            await UnitOfWork.SaveChangesAsync();

            Logger.LogInformation("Calendar {CalendarName} (ID: {CalendarId}) updated successfully", 
                _calendar.Name, _calendar.Id);

            CloseDialog();
            
            // Notify parent component
            await OnCalendarUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update calendar {CalendarId}", _calendar.Id);
            _errorMessage = $"Failed to save changes: {ex.Message}";
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }
    
    /// <summary>
    /// Determines if the field selection is configured as essential-only
    /// </summary>
    private static bool IsEssentialFieldSelection(CalendarFieldSelection fieldSelection)
    {
        return !fieldSelection.Attendees && !fieldSelection.Body;
    }
}
