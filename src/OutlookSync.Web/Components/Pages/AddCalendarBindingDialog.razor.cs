using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Dialog component for adding a new calendar binding with a wizard flow
/// </summary>
public partial class AddCalendarBindingDialog
{
    private bool _isDialogOpen;
    private int _currentStep = 1;
    
    // Step 1: Source selection
    private List<Credential>? _credentials;
    private string _selectedSourceCredentialId = string.Empty;
    private string _selectedSourceCalendar = string.Empty;
    private List<AvailableCalendar>? _sourceCalendars;
    private bool _isLoadingCredentials;
    private bool _isLoadingSourceCalendars;
    
    // Step 2: Target selection
    private string _selectedTargetCredentialId = string.Empty;
    private string _selectedTargetCalendar = string.Empty;
    private List<AvailableCalendar>? _targetCalendars;
    private bool _isLoadingTargetCalendars;
    
    // Step 3: Configuration
    private CalendarBindingSettingsForm? _settingsForm;
    
    // Step 4: Review and save
    private string _bindingName = string.Empty;
    private bool _isSaving;
    private string? _errorMessage;

    [Parameter]
    public EventCallback OnBindingAdded { get; set; }

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
        _selectedSourceCredentialId = string.Empty;
        _selectedSourceCalendar = string.Empty;
        _selectedTargetCredentialId = string.Empty;
        _selectedTargetCalendar = string.Empty;
        _sourceCalendars = null;
        _targetCalendars = null;
        _bindingName = string.Empty;
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
            LogFailedToLoadCredentials(Logger, ex);
            _errorMessage = "Failed to load credentials. Please try again.";
        }
        finally
        {
            _isLoadingCredentials = false;
        }
    }

    /// <summary>
    /// Loads available calendars for the source credential
    /// </summary>
    private async Task OnSourceCredentialChangedAsync()
    {
        _selectedSourceCalendar = string.Empty;
        _sourceCalendars = null;

        if (string.IsNullOrEmpty(_selectedSourceCredentialId))
        {
            return;
        }

        _isLoadingSourceCalendars = true;
        _errorMessage = null;
        
        try
        {
            var credential = _credentials?.FirstOrDefault(c => c.Id.ToString() == _selectedSourceCredentialId);
            if (credential == null)
            {
                return;
            }

            var repository = CalendarEventRepositoryFactory.Create(credential);
            await repository.InitAsync();
            
            var calendars = await repository.GetAvailableCalendarsAsync();
            _sourceCalendars = calendars.OrderBy(c => c.Name).ToList();
        }
        catch (Exception ex)
        {
            LogFailedToLoadSourceCalendars(Logger, ex);
            _errorMessage = $"Failed to load calendars: {ex.Message}";
        }
        finally
        {
            _isLoadingSourceCalendars = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Loads available calendars for the target credential
    /// </summary>
    private async Task OnTargetCredentialChangedAsync()
    {
        _selectedTargetCalendar = string.Empty;
        _targetCalendars = null;

        if (string.IsNullOrEmpty(_selectedTargetCredentialId))
        {
            return;
        }

        _isLoadingTargetCalendars = true;
        _errorMessage = null;
        
        try
        {
            var credential = _credentials?.FirstOrDefault(c => c.Id.ToString() == _selectedTargetCredentialId);
            if (credential == null)
            {
                return;
            }

            var repository = CalendarEventRepositoryFactory.Create(credential);
            await repository.InitAsync();
            
            var calendars = await repository.GetAvailableCalendarsAsync();
            _targetCalendars = calendars.OrderBy(c => c.Name).ToList();
        }
        catch (Exception ex)
        {
            LogFailedToLoadTargetCalendars(Logger, ex);
            _errorMessage = $"Failed to load calendars: {ex.Message}";
        }
        finally
        {
            _isLoadingTargetCalendars = false;
            StateHasChanged();
        }
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
        _errorMessage = null;
        
        if (_currentStep == 4)
        {
            // Generate default binding name
            var sourceName = GetCalendarName(_sourceCalendars, _selectedSourceCalendar);
            var targetName = GetCalendarName(_targetCalendars, _selectedTargetCalendar);
            _bindingName = $"{sourceName} → {targetName}";
        }
        
        StateHasChanged();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Moves to the previous step
    /// </summary>
    private void PreviousStep()
    {
        if (_currentStep > 1)
        {
            _currentStep--;
            _errorMessage = null;
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
            1 => !string.IsNullOrEmpty(_selectedSourceCredentialId) && 
                 !string.IsNullOrEmpty(_selectedSourceCalendar),
            2 => !string.IsNullOrEmpty(_selectedTargetCredentialId) && 
                 !string.IsNullOrEmpty(_selectedTargetCalendar),
            3 => true,
            _ => false
        };
    }

    /// <summary>
    /// Saves the calendar binding to the database
    /// </summary>
    private async Task SaveBinding()
    {
        if (string.IsNullOrWhiteSpace(_bindingName) || _settingsForm == null)
        {
            return;
        }

        _isSaving = true;
        _errorMessage = null;

        try
        {
            var sourceCredentialId = Guid.Parse(_selectedSourceCredentialId);
            var targetCredentialId = Guid.Parse(_selectedTargetCredentialId);

            // Check for duplicates
            var exists = await CalendarBindingRepository.ExistsAsync(
                sourceCredentialId, _selectedSourceCalendar,
                targetCredentialId, _selectedTargetCalendar);

            if (exists)
            {
                _errorMessage = "A binding with this source and target already exists.";
                return;
            }

            // Check if source and target are the same
            if (sourceCredentialId == targetCredentialId && _selectedSourceCalendar == _selectedTargetCalendar)
            {
                _errorMessage = "Source and target cannot be the same calendar.";
                return;
            }

            var configuration = _settingsForm.GetConfiguration();

            var binding = new CalendarBinding
            {
                Name = _bindingName,
                SourceCredentialId = sourceCredentialId,
                SourceCalendarExternalId = _selectedSourceCalendar,
                TargetCredentialId = targetCredentialId,
                TargetCalendarExternalId = _selectedTargetCalendar,
                Configuration = configuration
            };

            await CalendarBindingRepository.AddAsync(binding);
            await UnitOfWork.SaveChangesAsync();

            LogBindingAdded(Logger, binding.Name, binding.Id);

            CloseDialog();
            
            await OnBindingAdded.InvokeAsync();
        }
        catch (Exception ex)
        {
            LogFailedToSaveBinding(Logger, ex);
            _errorMessage = $"Failed to save binding: {ex.Message}";
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
        if (stepNumber <= _currentStep)
        {
            return "bg-outlook-blue text-white";
        }
        else
        {
            return "bg-gray-300 text-gray-600";
        }
    }

    /// <summary>
    /// Gets the credential name by ID
    /// </summary>
    private string GetCredentialName(string? credentialId)
    {
        if (string.IsNullOrEmpty(credentialId) || _credentials == null)
        {
            return "Unknown";
        }

        var credential = _credentials.FirstOrDefault(c => c.Id.ToString() == credentialId);
        return credential?.FriendlyName ?? "Unknown";
    }

    /// <summary>
    /// Gets the calendar name by external ID
    /// </summary>
    private static string GetCalendarName(List<AvailableCalendar>? calendars, string? externalId)
    {
        if (string.IsNullOrEmpty(externalId) || calendars == null)
        {
            return "Unknown";
        }

        var calendar = calendars.FirstOrDefault(c => c.ExternalId == externalId);
        return calendar?.Name ?? externalId;
    }
}
