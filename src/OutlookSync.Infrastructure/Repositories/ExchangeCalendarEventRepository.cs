using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.ValueObjects;
using EwsExchangeService = Microsoft.Exchange.WebServices.Data.ExchangeService;
using DomainCalendarEvent = OutlookSync.Domain.ValueObjects.CalendarEvent;

namespace OutlookSync.Infrastructure.Repositories;

/// <summary>
/// Exchange Web Services implementation of ICalendarEventRepository
/// </summary>
public class ExchangeCalendarEventRepository : ICalendarEventRepository
{
    private const string CopiedFromMarker = "[Copied from:";
    private const int DefaultPastDaysToRetrieve = 7;
    private const int DefaultFutureDaysToRetrieve = 30;
    
    private readonly EwsExchangeService _service;
    private readonly Guid _calendarId;
    private readonly ILogger<ExchangeCalendarEventRepository> _logger;
    private readonly RetryPolicy _retryPolicy;

    public ExchangeCalendarEventRepository(
        string accessToken,
        string serviceUrl,
        Guid calendarId,
        string calendarExternalId,
        ILogger<ExchangeCalendarEventRepository> logger,
        RetryPolicy? retryPolicy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceUrl, nameof(serviceUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarExternalId, nameof(calendarExternalId));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _calendarId = calendarId;
        _logger = logger;
        _retryPolicy = retryPolicy ?? RetryPolicy.CreateDefault();

        _service = new EwsExchangeService(ExchangeVersion.Exchange2016)
        {
            Url = new Uri(serviceUrl),
            Credentials = new OAuthCredentials(accessToken),
            Timeout = 100000 // 100 seconds
        };

        _logger.LogInformation(
            "ExchangeCalendarEventRepository initialized for calendar {CalendarId}",
            calendarId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DomainCalendarEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all events for calendar {CalendarId}", _calendarId);

        return await ExecuteWithRetryAsync(async () =>
        {
            // Get events for a configurable time window
            var startDate = DateTime.Now.AddDays(-DefaultPastDaysToRetrieve);
            var endDate = DateTime.Now.AddDays(DefaultFutureDaysToRetrieve);

            var view = new CalendarView(startDate, endDate)
            {
                PropertySet = CreatePropertySet()
            };

            var folderId = GetCalendarFolderId();
            var appointments = await System.Threading.Tasks.Task.Run(
                () => _service.FindAppointments(folderId, view),
                cancellationToken);

            _logger.LogInformation("Found {Count} appointments", appointments.Items.Count);

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
    public async Task<DomainCalendarEvent?> FindCopiedEventAsync(
        DomainCalendarEvent sourceEvent,
        Calendar sourceCalendar,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceEvent, nameof(sourceEvent));
        ArgumentNullException.ThrowIfNull(sourceCalendar, nameof(sourceCalendar));

        _logger.LogInformation(
            "Finding copied event from source calendar {SourceCalendarId} with original event ID {OriginalEventId}",
            sourceCalendar.Id,
            sourceEvent.ExternalId);

        // Get all events and search for the copied one
        var allEvents = await GetAllAsync(cancellationToken);

        var copiedEvent = allEvents.FirstOrDefault(e =>
            e.IsCopiedEvent &&
            e.OriginalEventId == sourceEvent.ExternalId &&
            e.SourceCalendarId == sourceCalendar.Id);

        if (copiedEvent != null)
        {
            _logger.LogInformation("Found copied event with ID {EventId}", copiedEvent.Id);
        }
        else
        {
            _logger.LogInformation("No copied event found");
        }

        return copiedEvent;
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task AddAsync(DomainCalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent, nameof(calendarEvent));

        _logger.LogInformation(
            "Adding calendar event '{Subject}' to calendar {CalendarId}",
            calendarEvent.Subject,
            _calendarId);

        await ExecuteWithRetryAsync(async () =>
        {
            var appointment = new Appointment(_service)
            {
                Subject = calendarEvent.Subject,
                Body = new MessageBody(BodyType.Text, calendarEvent.Body ?? string.Empty),
                Start = calendarEvent.Start,
                End = calendarEvent.End,
                Location = calendarEvent.Location ?? string.Empty,
                IsAllDayEvent = calendarEvent.IsAllDay
            };

            // Store metadata about copied events in the body
            // Note: Extended properties would be better but require additional setup
            if (calendarEvent.IsCopiedEvent && calendarEvent.OriginalEventId != null)
            {
                var metadata = $"\n\n{CopiedFromMarker} {calendarEvent.OriginalEventId}]";
                appointment.Body = new MessageBody(
                    BodyType.Text,
                    (calendarEvent.Body ?? string.Empty) + metadata);
            }

            var folderId = GetCalendarFolderId();
            await System.Threading.Tasks.Task.Run(() => appointment.Save(folderId), cancellationToken);

            _logger.LogInformation(
                "Calendar event created with ID: {EventId}",
                appointment.Id.UniqueId);
        }, cancellationToken);
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
    /// Gets the folder ID for the calendar
    /// </summary>
    private static FolderId GetCalendarFolderId()
    {
        // For now, use the default calendar
        // TODO: Support custom calendar folders
        return new FolderId(WellKnownFolderName.Calendar);
    }

    /// <summary>
    /// Maps Exchange Appointment to domain CalendarEvent
    /// </summary>
    private DomainCalendarEvent MapToCalendarEvent(Appointment appointment)
    {
        // Check if this is a copied event by looking for metadata in body
        var body = appointment.Body.Text ?? string.Empty;
        var isCopiedEvent = body.Contains(CopiedFromMarker, StringComparison.Ordinal);
        string? originalEventId = null;

        if (isCopiedEvent)
        {
            var startIndex = body.IndexOf(CopiedFromMarker, StringComparison.Ordinal);
            var endIndex = body.IndexOf("]", startIndex, StringComparison.Ordinal);
            if (startIndex >= 0 && endIndex > startIndex)
            {
                var metadataStart = startIndex + CopiedFromMarker.Length;
                originalEventId = body.Substring(metadataStart, endIndex - metadataStart).Trim();
                // Remove metadata from body
                body = body.Substring(0, startIndex).Trim();
            }
        }

        return new DomainCalendarEvent
        {
            Id = Guid.NewGuid(), // Generate new ID for domain
            ExternalId = appointment.Id.UniqueId,
            Subject = appointment.Subject,
            Body = body,
            Start = appointment.Start,
            End = appointment.End,
            Location = appointment.Location,
            IsAllDay = appointment.IsAllDayEvent,
            IsRecurring = appointment.IsRecurring,
            Organizer = appointment.Organizer?.Address,
            CalendarId = _calendarId,
            IsCopiedEvent = isCopiedEvent,
            OriginalEventId = originalEventId,
            // Note: SourceCalendarId cannot be reliably determined from current metadata
            // Would require using extended properties for proper implementation
            SourceCalendarId = isCopiedEvent ? null : null
        };
    }
}
