using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Infrastructure.Authentication;
using EwsExchangeService = Microsoft.Exchange.WebServices.Data.ExchangeService;
using DomainCalendarEvent = OutlookSync.Domain.ValueObjects.CalendarEvent;
using Task = System.Threading.Tasks.Task;

namespace OutlookSync.Infrastructure.Repositories;

/// <summary>
/// Exchange Web Services implementation of ICalendarEventRepository
/// </summary>
public class ExchangeCalendarEventRepository : ICalendarEventRepository
{
    private const int DefaultPastDaysToRetrieve = 7;
    private const int DefaultFutureDaysToRetrieve = 30;
    private const string ExchangeServiceUrl = "https://outlook.office365.com/EWS/Exchange.asmx";

    // Extended properties for storing sync metadata
    private static readonly Guid PropertySetId = new("{C11FF724-AA03-4555-9952-8FA248A11C3E}");
    private static readonly ExtendedPropertyDefinition OriginalEventIdProperty =
        new(PropertySetId, "OriginalEventId", MapiPropertyType.String);
    private static readonly ExtendedPropertyDefinition SourceCalendarBindingIdProperty =
        new(PropertySetId, "SourceCalendarBindingId", MapiPropertyType.String);

    private static readonly PropertySet s_fullPropertySet = new(BasePropertySet.FirstClassProperties)
    {
        OriginalEventIdProperty,
        SourceCalendarBindingIdProperty
    };

    private readonly Credential _credential;
    private readonly ILogger<ExchangeCalendarEventRepository> _logger;
    private readonly RetryPolicy _retryPolicy;
    private EwsExchangeService? _service;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeCalendarEventRepository"/> class.
    /// </summary>
    /// <param name="credential">The credential for authentication.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="retryPolicy">The retry policy for transient failures (optional).</param>
    public ExchangeCalendarEventRepository(
        Credential credential,
        ILogger<ExchangeCalendarEventRepository> logger,
        RetryPolicy? retryPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(credential, nameof(credential));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _credential = credential;
        _logger = logger;
        _retryPolicy = retryPolicy ?? RetryPolicy.CreateDefault();

        _logger.LogDebug("ExchangeCalendarEventRepository created");
    }

    /// <inheritdoc/>
    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            _logger.LogDebug("Repository already initialized");
            return;
        }

        _logger.LogInformation("Initializing repository and authenticating");

        try
        {
            // Build MSAL Public Client Application
            var app = MsalHelper.CreatePublicClientApplication();

            // Configure token cache serialization
            MsalHelper.ConfigureTokenCache(
                app,
                getStatusData: () => _credential.StatusData,
                updateStatusData: data =>
                {
                    _credential.UpdateStatusData(data);
                    _logger.LogDebug("Token cache updated and saved to credential");
                });

            // Try to acquire token silently
            AuthenticationResult result;
            var accounts = await app.GetAccountsAsync();
            
            if (!accounts.Any())
            {
                _logger.LogError("No cached accounts found");
                _credential.MarkTokenAsInvalid();
                throw new InvalidOperationException("No cached authentication found. Please authenticate first.");
            }

            _logger.LogDebug("Attempting silent token acquisition");
            try
            {
                result = await app.AcquireTokenSilent(MsalHelper.EwsScopes, accounts.FirstOrDefault())
                    .ExecuteAsync(cancellationToken);
                _logger.LogInformation("Token acquired silently");
            }
            catch (MsalUiRequiredException ex)
            {
                _logger.LogError(ex, "Silent token acquisition failed - user interaction required");
                _credential.MarkTokenAsInvalid();
                throw new InvalidOperationException("Token expired or invalid. User interaction required for re-authentication.", ex);
            }

            // Initialize EWS service with the acquired token
            _service = new EwsExchangeService(ExchangeVersion.Exchange2016)
            {
                Url = new Uri(ExchangeServiceUrl),
                Credentials = new OAuthCredentials(result.AccessToken),
                Timeout = 100000 // 100 seconds
            };

            _isInitialized = true;
            _logger.LogInformation("Repository successfully initialized and authenticated");
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "MSAL authentication failed");
            _credential.MarkTokenAsInvalid();
            throw new InvalidOperationException("Failed to authenticate", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize repository");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DomainCalendarEvent>> GetAllAsync(string calendarExternalId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarExternalId, nameof(calendarExternalId));
        
        _logger.LogInformation("Getting all events for calendar {CalendarExternalId}", calendarExternalId);

        return await ExecuteWithRetryAsync(async () =>
        {
            // Get events for a configurable time window
            var startDate = DateTime.Now.AddDays(-DefaultPastDaysToRetrieve);
            var endDate = DateTime.Now.AddDays(DefaultFutureDaysToRetrieve);

            var view = new CalendarView(startDate, endDate)
            {
                PropertySet = new PropertySet(BasePropertySet.IdOnly)
            };

            var folderId = new FolderId(calendarExternalId);
            var appointments = await _service!.FindAppointments(folderId, view);

            _logger.LogInformation("Found {Count} appointments", appointments.Items.Count);

            var events = new List<DomainCalendarEvent>();
            foreach (var appointment in appointments)
            {
                await appointment.Load(s_fullPropertySet, cancellationToken);
                var calendarEvent = MapToCalendarEvent(appointment);
                events.Add(calendarEvent);
            }

            return (IReadOnlyList<DomainCalendarEvent>)events;
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AvailableCalendar>> GetAvailableCalendarsAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        _logger.LogInformation("Getting available calendars for authenticated user");

        return await ExecuteWithRetryAsync(async () =>
        {
            var calendars = new List<AvailableCalendar>();

            // Get the root calendar folder
            var rootCalendarFolder = await Microsoft.Exchange.WebServices.Data.Folder.Bind(
                _service!,
                WellKnownFolderName.Calendar,
                new PropertySet(BasePropertySet.IdOnly, FolderSchema.DisplayName),
                cancellationToken);

            // Add the default calendar
            calendars.Add(new AvailableCalendar
            {
                ExternalId = rootCalendarFolder.Id.UniqueId,
                Name = rootCalendarFolder.DisplayName
            });

            // Find all calendar subfolders
            var folderView = new FolderView(int.MaxValue)
            {
                PropertySet = new PropertySet(BasePropertySet.IdOnly, FolderSchema.DisplayName),
                Traversal = FolderTraversal.Deep
            };

            var findResults = await _service!.FindFolders(
                WellKnownFolderName.Calendar, 
                new SearchFilter.IsEqualTo(FolderSchema.FolderClass, "IPF.Appointment"), 
                folderView);

            foreach (var folder in findResults.Folders)
            {
                if (folder.Id.UniqueId != rootCalendarFolder.Id.UniqueId)
                {
                    calendars.Add(new AvailableCalendar
                    {
                        ExternalId = folder.Id.UniqueId,
                        Name = folder.DisplayName
                    });
                }
            }

            _logger.LogInformation("Found {Count} available calendars", calendars.Count);
            return (IReadOnlyList<AvailableCalendar>)calendars;
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AvailableCalendar?> GetAvailableCalendarByIdAsync(string calendarExternalId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarExternalId, nameof(calendarExternalId));
        
        _logger.LogInformation("Getting calendar with external ID {CalendarExternalId}", calendarExternalId);

        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                // Try to bind directly to the folder using the unique ID
                var folder = await Microsoft.Exchange.WebServices.Data.Folder.Bind(
                    _service!,
                    new FolderId(calendarExternalId),
                    new PropertySet(BasePropertySet.IdOnly, FolderSchema.DisplayName),
                    cancellationToken);

                _logger.LogInformation("Found calendar: {CalendarName}", folder.DisplayName);
                
                return new AvailableCalendar
                {
                    ExternalId = folder.Id.UniqueId,
                    Name = folder.DisplayName
                };
            }
            catch (ServiceResponseException ex)
            {
                _logger.LogWarning(ex, "Calendar with ID {CalendarExternalId} not found", calendarExternalId);
                return null;
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DomainCalendarEvent?> FindCopiedEventAsync(
        string originalEventExternalId,
        Guid sourceCalendarBindingId,
        string targetCalendarExternalId,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        ArgumentException.ThrowIfNullOrEmpty(originalEventExternalId, nameof(originalEventExternalId));
        ArgumentException.ThrowIfNullOrEmpty(targetCalendarExternalId, nameof(targetCalendarExternalId));

        _logger.LogInformation(
            "Finding copied event in target calendar {TargetCalendarExternalId} from source calendar binding {SourceCalendarBindingId} with original event ID {OriginalEventId}",
            targetCalendarExternalId,
            sourceCalendarBindingId,
            originalEventExternalId);

        return await ExecuteWithRetryAsync(async () =>
        {
            // Create filters for extended properties
            var originalEventIdFilter = new SearchFilter.IsEqualTo(
                OriginalEventIdProperty,
                originalEventExternalId);

            var sourceCalendarBindingIdFilter = new SearchFilter.IsEqualTo(
                SourceCalendarBindingIdProperty,
                sourceCalendarBindingId.ToString());

            // Combine filters with AND
            var combinedFilter = new SearchFilter.SearchFilterCollection(
                LogicalOperator.And,
                originalEventIdFilter,
                sourceCalendarBindingIdFilter);

            // Create view with extended properties
            var view = new ItemView(1)
            {
                PropertySet = new PropertySet(
                    BasePropertySet.IdOnly,
                    OriginalEventIdProperty,
                    SourceCalendarBindingIdProperty)
            };

            var folderId = new FolderId(targetCalendarExternalId);
            var results = await _service!.FindItems(folderId, combinedFilter, view);

            if (results.Items.Count > 0 && results.Items[0] is Appointment appointment)
            {
                await appointment.Load(s_fullPropertySet, cancellationToken);
                var copiedEvent = MapToCalendarEvent(appointment);
                _logger.LogInformation("Found copied event with ID {EventId}", copiedEvent.Id);
                return copiedEvent;
            }

            _logger.LogInformation("No copied event found");
            return null;
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task AddAsync(DomainCalendarEvent calendarEvent, string calendarExternalId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(calendarEvent, nameof(calendarEvent));
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarExternalId, nameof(calendarExternalId));

        _logger.LogInformation(
            "Adding calendar event '{Subject}' to calendar {CalendarExternalId}",
            calendarEvent.Subject,
            calendarExternalId);

        await ExecuteWithRetryAsync(async () =>
        {
            var appointment = new Appointment(_service!)
            {
                Subject = calendarEvent.Subject,
                Body = new MessageBody(
                    calendarEvent.BodyType == CalendarEventBodyType.Html ? BodyType.HTML : BodyType.Text,
                    calendarEvent.Body ?? string.Empty),
                Start = calendarEvent.Start,
                End = calendarEvent.End,
                IsAllDayEvent = calendarEvent.IsAllDay
            };

            // Set location
            appointment.Location = calendarEvent.Location ?? string.Empty;

            // Set status (Free/Busy/Tentative/etc.)
            appointment.LegacyFreeBusyStatus = calendarEvent.Status switch
            {
                EventStatus.Free => LegacyFreeBusyStatus.Free,
                EventStatus.Tentative => LegacyFreeBusyStatus.Tentative,
                EventStatus.Busy => LegacyFreeBusyStatus.Busy,
                EventStatus.OutOfOffice => LegacyFreeBusyStatus.OOF,
                EventStatus.WorkingElsewhere => LegacyFreeBusyStatus.WorkingElsewhere,
                _ => LegacyFreeBusyStatus.Busy
            };

            // Set sensitivity (privacy)
            if (calendarEvent.IsPrivate)
            {
                appointment.Sensitivity = Sensitivity.Private;
            }

            // Set categories if present
            if (!string.IsNullOrWhiteSpace(calendarEvent.Categories))
            {
                var categoryList = calendarEvent.Categories
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
                foreach (var category in categoryList)
                {
                    appointment.Categories.Add(category);
                }
            }

            // Add required attendees
            if (calendarEvent.RequiredAttendees.Count > 0)
            {
                foreach (var email in calendarEvent.RequiredAttendees)
                {
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        appointment.RequiredAttendees.Add(new Attendee { Address = email });
                    }
                }
            }

            // Add optional attendees
            if (calendarEvent.OptionalAttendees.Count > 0)
            {
                foreach (var email in calendarEvent.OptionalAttendees)
                {
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        appointment.OptionalAttendees.Add(new Attendee { Address = email });
                    }
                }
            }

            // Set reminders
            if (calendarEvent.ReminderMinutesBeforeStart.HasValue)
            {
                appointment.IsReminderSet = true;
                appointment.ReminderMinutesBeforeStart = calendarEvent.ReminderMinutesBeforeStart.Value;
            }
            else
            {
                appointment.IsReminderSet = false;
            }

            // Store metadata about copied events using extended properties
            if (calendarEvent.IsCopiedEvent && calendarEvent.OriginalEventId != null)
            {
                appointment.SetExtendedProperty(OriginalEventIdProperty, calendarEvent.OriginalEventId);

                if (calendarEvent.SourceCalendarBindingId.HasValue)
                {
                    appointment.SetExtendedProperty(SourceCalendarBindingIdProperty, calendarEvent.SourceCalendarBindingId.Value.ToString());
                }
            }

            var folderId = new FolderId(calendarExternalId);
            await appointment.Save(folderId);

            calendarEvent.ExternalId = appointment.Id.UniqueId;

            _logger.LogInformation(
                "Calendar event created with ID: {EventId}",
                appointment.Id.UniqueId);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task UpdateAsync(DomainCalendarEvent calendarEvent, string calendarExternalId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(calendarEvent, nameof(calendarEvent));
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarExternalId, nameof(calendarExternalId));
        
        if (string.IsNullOrWhiteSpace(calendarEvent.ExternalId))
        {
            throw new ArgumentException("CalendarEvent must have an ExternalId to be updated", nameof(calendarEvent));
        }

        _logger.LogInformation(
            "Updating calendar event '{Subject}' with ID {EventId}",
            calendarEvent.Subject,
            calendarEvent.ExternalId);

        await ExecuteWithRetryAsync(async () =>
        {
            // Bind to the existing appointment
            var appointment = await Appointment.Bind(_service!, new ItemId(calendarEvent.ExternalId), s_fullPropertySet);

            // Update basic properties
            appointment.Subject = calendarEvent.Subject;
            appointment.Body = new MessageBody(
                calendarEvent.BodyType == CalendarEventBodyType.Html ? BodyType.HTML : BodyType.Text,
                calendarEvent.Body ?? string.Empty);
            appointment.Start = calendarEvent.Start;
            appointment.End = calendarEvent.End;
            appointment.IsAllDayEvent = calendarEvent.IsAllDay;

            // Update location
            appointment.Location = calendarEvent.Location ?? string.Empty;

            // Update status (Free/Busy/Tentative/etc.)
            appointment.LegacyFreeBusyStatus = calendarEvent.Status switch
            {
                EventStatus.Free => LegacyFreeBusyStatus.Free,
                EventStatus.Tentative => LegacyFreeBusyStatus.Tentative,
                EventStatus.Busy => LegacyFreeBusyStatus.Busy,
                EventStatus.OutOfOffice => LegacyFreeBusyStatus.OOF,
                EventStatus.WorkingElsewhere => LegacyFreeBusyStatus.WorkingElsewhere,
                _ => LegacyFreeBusyStatus.Busy
            };

            // Update sensitivity (privacy)
            appointment.Sensitivity = calendarEvent.IsPrivate ? Sensitivity.Private : Sensitivity.Normal;

            // Update categories
            appointment.Categories.Clear();
            if (!string.IsNullOrWhiteSpace(calendarEvent.Categories))
            {
                var categoryList = calendarEvent.Categories
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
                foreach (var category in categoryList)
                {
                    appointment.Categories.Add(category);
                }
            }

            // Update attendees
            appointment.RequiredAttendees.Clear();
            appointment.OptionalAttendees.Clear();
            
            // Add required attendees
            if (calendarEvent.RequiredAttendees.Count > 0)
            {
                foreach (var email in calendarEvent.RequiredAttendees)
                {
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        appointment.RequiredAttendees.Add(new Attendee { Address = email });
                    }
                }
            }

            // Add optional attendees
            if (calendarEvent.OptionalAttendees.Count > 0)
            {
                foreach (var email in calendarEvent.OptionalAttendees)
                {
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        appointment.OptionalAttendees.Add(new Attendee { Address = email });
                    }
                }
            }

            // Update reminders
            if (calendarEvent.ReminderMinutesBeforeStart.HasValue)
            {
                appointment.IsReminderSet = true;
                appointment.ReminderMinutesBeforeStart = calendarEvent.ReminderMinutesBeforeStart.Value;
            }
            else
            {
                appointment.IsReminderSet = false;
            }

            // Update extended properties for copied events
            if (calendarEvent.IsCopiedEvent && calendarEvent.OriginalEventId != null)
            {
                appointment.SetExtendedProperty(OriginalEventIdProperty, calendarEvent.OriginalEventId);
                if (calendarEvent.SourceCalendarBindingId.HasValue)
                {
                    appointment.SetExtendedProperty(SourceCalendarBindingIdProperty, calendarEvent.SourceCalendarBindingId.Value.ToString());
                }
            }

            // Save changes
            await appointment.Update(ConflictResolutionMode.AlwaysOverwrite);

            _logger.LogInformation(
                "Calendar event updated successfully: {EventId}",
                calendarEvent.ExternalId);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DomainCalendarEvent>> GetCopiedEventsAsync(
        Guid sourceCalendarBindingId,
        string targetCalendarExternalId,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        ArgumentException.ThrowIfNullOrEmpty(targetCalendarExternalId, nameof(targetCalendarExternalId));

        _logger.LogInformation(
            "Getting all copied events in target calendar {TargetCalendarExternalId} from source calendar binding {SourceCalendarBindingId}",
            targetCalendarExternalId,
            sourceCalendarBindingId);

        return await ExecuteWithRetryAsync(async () =>
        {
            var sourceCalendarBindingIdFilter = new SearchFilter.IsEqualTo(
                SourceCalendarBindingIdProperty,
                sourceCalendarBindingId.ToString());

            var view = new ItemView(1000)
            {
                PropertySet = new PropertySet(
                    BasePropertySet.IdOnly,
                    OriginalEventIdProperty,
                    SourceCalendarBindingIdProperty)
            };

            var folderId = new FolderId(targetCalendarExternalId);
            var copiedEvents = new List<DomainCalendarEvent>();
            
            FindItemsResults<Item>? results;
            do
            {
                results = await _service!.FindItems(folderId, sourceCalendarBindingIdFilter, view);
                
                foreach (var item in results.Items.OfType<Appointment>())
                {
                    await item.Load(s_fullPropertySet, cancellationToken);
                    copiedEvents.Add(MapToCalendarEvent(item));
                }
                
                view.Offset += results.Items.Count;
            } while (results.MoreAvailable);

            _logger.LogInformation("Found {Count} copied events", copiedEvents.Count);
            return (IReadOnlyList<DomainCalendarEvent>)copiedEvents;
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string eventExternalId, string calendarExternalId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrEmpty(eventExternalId, nameof(eventExternalId));
        ArgumentException.ThrowIfNullOrEmpty(calendarExternalId, nameof(calendarExternalId));

        _logger.LogInformation(
            "Deleting event {EventExternalId} from calendar {CalendarExternalId}",
            eventExternalId,
            calendarExternalId);

        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                var appointment = await Appointment.Bind(_service!, new ItemId(eventExternalId));
                await appointment.Delete(DeleteMode.MoveToDeletedItems);
                
                _logger.LogInformation("Event {EventExternalId} deleted successfully", eventExternalId);
                return true;
            }
            catch (ServiceResponseException ex) when (ex.ErrorCode == ServiceError.ErrorItemNotFound)
            {
                _logger.LogWarning("Event {EventExternalId} not found, possibly already deleted", eventExternalId);
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Ensures the repository has been initialized before use
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the repository has not been initialized</exception>
    private void EnsureInitialized()
    {
        if (!_isInitialized || _service == null)
        {
            throw new InvalidOperationException("Repository has not been initialized. Call InitAsync() first.");
        }
    }

    /// <summary>
    /// Executes an operation with retry logic and exponential backoff
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        // Attempt 0 is the initial try, then we retry up to MaxRetryAttempts times
        for (var attemptNumber = 0; attemptNumber <= _retryPolicy.MaxRetryAttempts; attemptNumber++)
        {
            try
            {
                return await operation();
            }
            catch (ServiceResponseException ex) when (IsTransientError(ex))
            {
                lastException = ex;

                // If we've exhausted all retries, break
                if (attemptNumber >= _retryPolicy.MaxRetryAttempts)
                {
                    _logger.LogError(
                        ex,
                        "Operation failed after initial attempt and {Attempts} retry attempts",
                        attemptNumber);
                    break;
                }

                var delay = _retryPolicy.CalculateDelay(attemptNumber);
                _logger.LogWarning(
                    ex,
                    "Transient error occurred (attempt {Attempt}/{TotalAttempts}). Retrying in {Delay}ms...",
                    attemptNumber + 1,
                    _retryPolicy.MaxRetryAttempts + 1,
                    delay);

                await System.Threading.Tasks.Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-retryable error occurred: {Message}", ex.Message);
                throw;
            }
        }

        throw new InvalidOperationException(
            $"Operation failed after initial attempt and {_retryPolicy.MaxRetryAttempts} retry attempts",
            lastException);
    }

    /// <summary>
    /// Executes an operation with retry logic (void version)
    /// </summary>
    private async System.Threading.Tasks.Task ExecuteWithRetryAsync(
        Func<System.Threading.Tasks.Task> operation,
        CancellationToken cancellationToken)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    /// <summary>
    /// Determines if an exception represents a transient error that can be retried
    /// </summary>
    private static bool IsTransientError(ServiceResponseException ex)
    {
        return ex.ErrorCode is
            ServiceError.ErrorServerBusy or
            ServiceError.ErrorTimeoutExpired or
            ServiceError.ErrorConnectionFailed or
            ServiceError.ErrorInternalServerTransientError;
    }

    /// <summary>
    /// Maps Exchange Appointment to domain CalendarEvent
    /// </summary>
    private static DomainCalendarEvent MapToCalendarEvent(Appointment appointment)
    {
        // Read metadata from extended properties
        var hasOriginalEventId = appointment.TryGetProperty(OriginalEventIdProperty, out string? originalEventId);
        var hasSourceCalendarBindingId = appointment.TryGetProperty(SourceCalendarBindingIdProperty, out string? sourceCalendarBindingIdStr);

        Guid? sourceCalendarBindingId = null;
        if (hasSourceCalendarBindingId && !string.IsNullOrEmpty(sourceCalendarBindingIdStr) && Guid.TryParse(sourceCalendarBindingIdStr, out var parsedId))
        {
            sourceCalendarBindingId = parsedId;
        }

        var isCopiedEvent = hasOriginalEventId && !string.IsNullOrEmpty(originalEventId);
        
        // Determine body type from Exchange appointment
        var bodyType = appointment.Body.BodyType == BodyType.HTML 
            ? CalendarEventBodyType.Html 
            : CalendarEventBodyType.Text;

        // Map LegacyFreeBusyStatus to EventStatus
        var status = appointment.LegacyFreeBusyStatus switch
        {
            LegacyFreeBusyStatus.Free => EventStatus.Free,
            LegacyFreeBusyStatus.Tentative => EventStatus.Tentative,
            LegacyFreeBusyStatus.Busy => EventStatus.Busy,
            LegacyFreeBusyStatus.OOF => EventStatus.OutOfOffice,
            LegacyFreeBusyStatus.WorkingElsewhere => EventStatus.WorkingElsewhere,
            _ => EventStatus.Busy
        };

        // Map MyResponseType to RsvpResponse
        var rsvpStatus = appointment.MyResponseType switch
        {
            MeetingResponseType.Accept => RsvpResponse.Yes,
            MeetingResponseType.Tentative => RsvpResponse.Maybe,
            MeetingResponseType.Decline => RsvpResponse.No,
            _ => RsvpResponse.None // Unknown, Organizer, NoResponseReceived
        };

        // Separate required and optional attendees
        var requiredAttendees = appointment.RequiredAttendees?.Count > 0
            ? appointment.RequiredAttendees
                .Where(a => !string.IsNullOrWhiteSpace(a.Address))
                .Select(a => a.Address)
                .ToList()
            : (IReadOnlyList<string>)[];

        var optionalAttendees = appointment.OptionalAttendees?.Count > 0
            ? appointment.OptionalAttendees
                .Where(a => !string.IsNullOrWhiteSpace(a.Address))
                .Select(a => a.Address)
                .ToList()
            : (IReadOnlyList<string>)[];
        
        // Categories as comma-separated string
        var categories = appointment.Categories?.Count > 0
            ? string.Join(", ", appointment.Categories)
            : null;

        return new DomainCalendarEvent
        {
            Id = Guid.NewGuid(), // Generate new ID for domain
            ExternalId = appointment.Id.UniqueId,
            Subject = appointment.Subject,
            Body = appointment.Body.Text ?? string.Empty,
            BodyType = bodyType,
            Start = appointment.Start,
            End = appointment.End,
            Location = appointment.Location,
            IsOnlineMeeting = appointment.IsOnlineMeeting,
            IsMeeting = appointment.IsMeeting,
            IsAllDay = appointment.IsAllDayEvent,
            IsRecurring = appointment.IsRecurring,
            Organizer = appointment.Organizer?.Address,
            Status = status,
            RsvpStatus = rsvpStatus,
            IsPrivate = appointment.Sensitivity is Sensitivity.Private or Sensitivity.Personal or Sensitivity.Confidential,
            RequiredAttendees = requiredAttendees,
            OptionalAttendees = optionalAttendees,
            Categories = categories,
            HasAttachments = appointment.HasAttachments,
            ReminderMinutesBeforeStart = appointment.IsReminderSet ? appointment.ReminderMinutesBeforeStart : null,
            OriginalEventId = isCopiedEvent ? originalEventId : null,
            SourceCalendarBindingId = sourceCalendarBindingId
        };
    }
}
