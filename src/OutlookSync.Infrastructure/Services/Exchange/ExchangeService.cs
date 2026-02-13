using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Services;
using OutlookSync.Domain.ValueObjects;
using EwsExchangeService = Microsoft.Exchange.WebServices.Data.ExchangeService;
using DomainCalendarEvent = OutlookSync.Domain.ValueObjects.CalendarEvent;

namespace OutlookSync.Infrastructure.Services.Exchange;

/// <summary>
/// Implementation of Exchange Web Services with resilient I/O and retry logic
/// </summary>
public class ExchangeService(ILogger<ExchangeService> logger, RetryPolicy retryPolicy) 
    : IExchangeService
{
    private EwsExchangeService? _service;
    private readonly RetryPolicy _retryPolicy = retryPolicy ?? RetryPolicy.CreateDefault();
    
    /// <inheritdoc/>
    public System.Threading.Tasks.Task InitializeAsync(string accessToken, string serviceUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceUrl, nameof(serviceUrl));
        
        logger.LogInformation("Initializing Exchange service with URL: {ServiceUrl}", serviceUrl);
        
        _service = new EwsExchangeService(ExchangeVersion.Exchange2016)
        {
            Url = new Uri(serviceUrl),
            Credentials = new OAuthCredentials(accessToken),
            Timeout = 100000 // 100 seconds default timeout
        };
        
        logger.LogInformation("Exchange service initialized successfully");
        
        return System.Threading.Tasks.Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<IReadOnlyList<DomainCalendarEvent>> GetCalendarEventsAsync(
        string calendarId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarId, nameof(calendarId));
        
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be greater than or equal to start date");
        }
        
        logger.LogInformation(
            "Getting calendar events from {StartDate} to {EndDate} for calendar {CalendarId}",
            startDate, endDate, calendarId);
        
        return await ExecuteWithRetryAsync(async () =>
        {
            var view = new CalendarView(startDate, endDate)
            {
                PropertySet = CreatePropertySet()
            };
            
            var folderId = GetDefaultCalendarFolderId();
            var appointments = await System.Threading.Tasks.Task.Run(
                () => _service!.FindAppointments(folderId, view),
                cancellationToken);
            
            logger.LogInformation("Found {Count} appointments", appointments.Items.Count);
            
            var events = new List<DomainCalendarEvent>();
            foreach (var appointment in appointments)
            {
                var calendarEvent = MapToCalendarEvent(appointment);
                events.Add(calendarEvent);
            }
            
            return (IReadOnlyList<DomainCalendarEvent>)events;
        }, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<DomainCalendarEvent> CreateCalendarEventAsync(
        string calendarId,
        DomainCalendarEvent calendarEvent,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarId, nameof(calendarId));
        ArgumentNullException.ThrowIfNull(calendarEvent, nameof(calendarEvent));
        
        logger.LogInformation(
            "Creating calendar event '{Subject}' in calendar {CalendarId}",
            calendarEvent.Subject, calendarId);
        
        return await ExecuteWithRetryAsync(async () =>
        {
            var appointment = new Appointment(_service!)
            {
                Subject = calendarEvent.Subject,
                Body = new MessageBody(BodyType.Text, calendarEvent.Body ?? string.Empty),
                Start = calendarEvent.Start,
                End = calendarEvent.End,
                Location = calendarEvent.Location ?? string.Empty,
                IsAllDayEvent = calendarEvent.IsAllDay
            };
            
            var folderId = GetDefaultCalendarFolderId();
            await System.Threading.Tasks.Task.Run(() => appointment.Save(folderId), cancellationToken);
            
            logger.LogInformation(
                "Calendar event created with ID: {EventId}",
                appointment.Id.UniqueId);
            
            return calendarEvent with { ExternalId = appointment.Id.UniqueId };
        }, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task UpdateCalendarEventAsync(
        string calendarId,
        DomainCalendarEvent calendarEvent,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarId, nameof(calendarId));
        ArgumentNullException.ThrowIfNull(calendarEvent, nameof(calendarEvent));
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEvent.ExternalId, nameof(calendarEvent.ExternalId));
        
        logger.LogInformation(
            "Updating calendar event {EventId} in calendar {CalendarId}",
            calendarEvent.ExternalId, calendarId);
        
        await ExecuteWithRetryAsync(async () =>
        {
            var itemId = new ItemId(calendarEvent.ExternalId);
            var appointment = await System.Threading.Tasks.Task.Run(
                () => Appointment.Bind(_service!, itemId, CreatePropertySet()),
                cancellationToken);
            
            appointment.Subject = calendarEvent.Subject;
            appointment.Body = new MessageBody(BodyType.Text, calendarEvent.Body ?? string.Empty);
            appointment.Start = calendarEvent.Start;
            appointment.End = calendarEvent.End;
            appointment.Location = calendarEvent.Location ?? string.Empty;
            appointment.IsAllDayEvent = calendarEvent.IsAllDay;
            
            await System.Threading.Tasks.Task.Run(() => appointment.Update(ConflictResolutionMode.AlwaysOverwrite), cancellationToken);
            
            logger.LogInformation("Calendar event updated successfully");
        }, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task DeleteCalendarEventAsync(
        string calendarId,
        string eventExternalId,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarId, nameof(calendarId));
        ArgumentException.ThrowIfNullOrWhiteSpace(eventExternalId, nameof(eventExternalId));
        
        logger.LogInformation(
            "Deleting calendar event {EventId} from calendar {CalendarId}",
            eventExternalId, calendarId);
        
        await ExecuteWithRetryAsync(async () =>
        {
            var itemId = new ItemId(eventExternalId);
            var appointment = await System.Threading.Tasks.Task.Run(
                () => Appointment.Bind(_service!, itemId),
                cancellationToken);
            
            await System.Threading.Tasks.Task.Run(() => appointment.Delete(DeleteMode.MoveToDeletedItems), cancellationToken);
            
            logger.LogInformation("Calendar event deleted successfully");
        }, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_service is null)
        {
            logger.LogWarning("Cannot test connection: service not initialized");
            return false;
        }
        
        try
        {
            logger.LogInformation("Testing Exchange connection...");
            
            // Try to access the calendar folder to verify connection
            var folder = await System.Threading.Tasks.Task.Run(
                () => Folder.Bind(_service, WellKnownFolderName.Calendar),
                cancellationToken);
            
            logger.LogInformation("Connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connection test failed: {Message}", ex.Message);
            return false;
        }
    }
    
    /// <summary>
    /// Executes an operation with retry logic and exponential backoff
    /// </summary>
    private async System.Threading.Tasks.Task<T> ExecuteWithRetryAsync<T>(
        Func<System.Threading.Tasks.Task<T>> operation,
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
                    logger.LogError(
                        ex,
                        "Operation failed after initial attempt and {Attempts} retry attempts",
                        attemptNumber);
                    break;
                }
                
                var delay = _retryPolicy.CalculateDelay(attemptNumber);
                logger.LogWarning(
                    ex,
                    "Transient error occurred (attempt {Attempt}/{TotalAttempts}). Retrying in {Delay}ms...",
                    attemptNumber + 1,
                    _retryPolicy.MaxRetryAttempts + 1,
                    delay);
                
                await System.Threading.Tasks.Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Non-retryable error occurred: {Message}", ex.Message);
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
    /// Creates a property set with commonly needed appointment properties
    /// </summary>
    private static PropertySet CreatePropertySet()
    {
        return new PropertySet(
            BasePropertySet.FirstClassProperties,
            AppointmentSchema.Subject,
            AppointmentSchema.Start,
            AppointmentSchema.End,
            AppointmentSchema.Location,
            AppointmentSchema.Body,
            AppointmentSchema.IsAllDayEvent,
            AppointmentSchema.Organizer,
            AppointmentSchema.LastModifiedTime
        );
    }
    
    /// <summary>
    /// Gets the default calendar folder ID
    /// Currently always returns the default Calendar folder
    /// TODO: In the future, extend to support custom calendar IDs
    /// </summary>
    private static FolderId GetDefaultCalendarFolderId()
    {
        return new FolderId(WellKnownFolderName.Calendar);
    }
    
    /// <summary>
    /// Maps Exchange Appointment to domain CalendarEvent
    /// </summary>
    private static DomainCalendarEvent MapToCalendarEvent(Appointment appointment)
    {
        return new DomainCalendarEvent
        {
            Id = Guid.NewGuid(),
            ExternalId = appointment.Id.UniqueId,
            Subject = appointment.Subject,
            Body = appointment.Body.Text,
            Start = appointment.Start,
            End = appointment.End,
            Location = appointment.Location,
            IsAllDay = appointment.IsAllDayEvent,
            CalendarId = Guid.Empty, // Will be set by repository
            IsCopiedEvent = false
        };
    }
    
    /// <summary>
    /// Ensures the service is initialized before use
    /// </summary>
    private void EnsureInitialized()
    {
        if (_service is null)
        {
            throw new InvalidOperationException(
                "Exchange service is not initialized. Call InitializeAsync first.");
        }
    }
}
