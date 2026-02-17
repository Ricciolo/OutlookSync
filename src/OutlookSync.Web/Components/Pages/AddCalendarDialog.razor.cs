using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Web.Components.Shared;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Dialog component for adding a new calendar with a wizard flow
/// </summary>
public partial class AddCalendarDialog
{
    private bool _isDialogOpen;
    private int _currentStep = 1;
    
    // Step 1: Credential selection
    private List<Credential>? _credentials;
    private Credential? _selectedCredential;
    private bool _isLoadingCredentials;
    
    // Step 2: Calendar selection
    private List<AvailableCalendar>? _availableCalendars;
    private AvailableCalendar? _selectedCalendar;
    private bool _isLoadingCalendars;
    private string? _errorMessage;
    
    // Step 3: Configuration
    private CalendarSettingsModel _settings = new();
    private bool _isSaving;

    [Parameter]
    public EventCallback OnCalendarAdded { get; set; }

    /// <summary>
    /// Opens the dialog and starts the wizard
    /// </summary>
    public async Task OpenAsync()
    {
        _isDialogOpen = true;
        _currentStep = 1;
        await LoadCredentialsAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Closes the dialog and resets state
    /// </summary>
    private void CloseDialog()
    {
        _isDialogOpen = false;
        ResetWizard();
        StateHasChanged();
    }

    /// <summary>
    /// Resets the wizard to initial state
    /// </summary>
    private void ResetWizard()
    {
        _currentStep = 1;
        _selectedCredential = null;
        _selectedCalendar = null;
        _availableCalendars = null;
        _settings = new CalendarSettingsModel();
        _errorMessage = null;
    }

    /// <summary>
    /// Loads available credentials
    /// </summary>
    private async Task LoadCredentialsAsync()
    {
        _isLoadingCredentials = true;
        try
        {
            _credentials = await CredentialRepository.Query
                .Where(c => c.TokenStatus == TokenStatus.Valid)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load credentials");
            _errorMessage = "Failed to load credentials. Please try again.";
        }
        finally
        {
            _isLoadingCredentials = false;
        }
    }

    /// <summary>
    /// Selects a credential
    /// </summary>
    private void SelectCredential(Credential credential)
    {
        _selectedCredential = credential;
        StateHasChanged();
    }

    /// <summary>
    /// Loads available calendars for the selected credential
    /// </summary>
    private async Task LoadCalendarsAsync()
    {
        if (_selectedCredential == null)
        {
            return;
        }

        _isLoadingCalendars = true;
        _errorMessage = null;
        
        try
        {
            var repository = CalendarEventRepositoryFactory.Create(_selectedCredential, null);
            await repository.InitAsync();
            
            var calendars = await repository.GetAvailableCalendarsAsync();
            _availableCalendars = calendars.OrderBy(c => c.Name).ToList();

            Logger.LogInformation("Loaded {Count} calendars for credential {CredentialId}", 
                _availableCalendars.Count, _selectedCredential.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load calendars for credential {CredentialId}", _selectedCredential.Id);
            _errorMessage = $"Failed to load calendars: {ex.Message}";
        }
        finally
        {
            _isLoadingCalendars = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Selects a calendar
    /// </summary>
    private void SelectCalendar(AvailableCalendar calendar)
    {
        _selectedCalendar = calendar;
        _settings.CalendarName = calendar.Name;
        StateHasChanged();
    }

    /// <summary>
    /// Moves to the next step
    /// </summary>
    private async Task NextStep()
    {
        if (!CanProceedToNextStep())
        {
            return;
        }

        _currentStep++;
        
        if (_currentStep == 2)
        {
            await LoadCalendarsAsync();
        }
        
        StateHasChanged();
    }

    /// <summary>
    /// Moves to the previous step
    /// </summary>
    private void PreviousStep()
    {
        if (_currentStep > 1)
        {
            _currentStep--;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Checks if the wizard can proceed to the next step
    /// </summary>
    private bool CanProceedToNextStep()
    {
        return _currentStep switch
        {
            1 => _selectedCredential != null,
            2 => _selectedCalendar != null,
            _ => false
        };
    }

    /// <summary>
    /// Saves the calendar to the database
    /// </summary>
    private async Task SaveCalendar()
    {
        if (_selectedCredential == null || _selectedCalendar == null || string.IsNullOrWhiteSpace(_settings.CalendarName))
        {
            return;
        }

        _isSaving = true;
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

            var calendar = new Calendar
            {
                Name = _settings.CalendarName,
                ExternalId = _selectedCalendar.ExternalId,
                CredentialId = _selectedCredential.Id,
                Configuration = new SyncConfiguration
                {
                    Interval = syncInterval,
                    StartDate = _settings.StartDate,
                    SyncDaysForward = _settings.SyncDaysForward,
                    IsPrivate = _settings.IsPrivate,
                    FieldSelection = fieldSelection
                }
            };

            await CalendarRepository.AddAsync(calendar);
            await UnitOfWork.SaveChangesAsync();

            Logger.LogInformation("Calendar {CalendarName} added successfully with ID {CalendarId}", 
                calendar.Name, calendar.Id);

            CloseDialog();
            
            // Notify parent component
            await OnCalendarAdded.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save calendar");
            _errorMessage = $"Failed to save calendar: {ex.Message}";
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Gets the CSS class for a wizard step
    /// </summary>
    private string GetStepClass(int stepNumber)
    {
        if (stepNumber < _currentStep)
        {
            return "bg-outlook-blue text-white";
        }
        else if (stepNumber == _currentStep)
        {
            return "bg-outlook-blue text-white";
        }
        else
        {
            return "bg-gray-300 text-gray-600";
        }
    }

    /// <summary>
    /// Gets the CSS class for a credential card
    /// </summary>
    private string GetCredentialCardClass(Credential credential)
    {
        return _selectedCredential?.Id == credential.Id 
            ? "border-outlook-blue bg-blue-50" 
            : "border-gray-300";
    }

    /// <summary>
    /// Gets the CSS class for a calendar card
    /// </summary>
    private string GetCalendarCardClass(AvailableCalendar calendar)
    {
        return _selectedCalendar?.ExternalId == calendar.ExternalId 
            ? "border-outlook-blue bg-blue-50" 
            : "border-gray-300";
    }
}
