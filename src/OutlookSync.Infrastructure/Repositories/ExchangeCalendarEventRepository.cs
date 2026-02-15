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
    private const int DefaultPastDaysToRetrieve = 7;
    private const int DefaultFutureDaysToRetrieve = 30;
    private const string ExchangeServiceUrl = "https://outlook.office365.com/EWS/Exchange.asmx";

    // Extended properties for storing sync metadata
    private static readonly Guid PropertySetId = new("{C11FF724-AA03-4555-9952-8FA248A11C3E}");
    private static readonly ExtendedPropertyDefinition OriginalEventIdProperty =
        new(PropertySetId, "OriginalEventId", MapiPropertyType.String);
    private static readonly ExtendedPropertyDefinition SourceCalendarIdProperty =
        new(PropertySetId, "SourceCalendarId", MapiPropertyType.String);

    private static readonly PropertySet s_fullPropertySet = new(BasePropertySet.FirstClassProperties)
                                                            {
                                                                OriginalEventIdProperty,
                                                                SourceCalendarIdProperty
                                                            };

    private readonly EwsExchangeService _service;
    private readonly Calendar _calendar;
    private readonly ILogger<ExchangeCalendarEventRepository> _logger;
    private readonly RetryPolicy _retryPolicy;

    public ExchangeCalendarEventRepository(
        Calendar calendar,
        Credential credential,
        ILogger<ExchangeCalendarEventRepository> logger,
        RetryPolicy? retryPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(calendar, nameof(calendar));
        ArgumentNullException.ThrowIfNull(credential, nameof(credential));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        if (!credential.IsTokenValid())
        {
            throw new ArgumentException("Credential token is invalid or expired", nameof(credential));
        }

        if (string.IsNullOrEmpty(credential.AccessToken))
        {
            throw new ArgumentException("Access token is missing", nameof(credential));
        }

        _calendar = calendar;
        _logger = logger;
        _retryPolicy = retryPolicy ?? RetryPolicy.CreateDefault();

        _service = new EwsExchangeService(ExchangeVersion.Exchange2016)
        {
            Url = new Uri(ExchangeServiceUrl),
            Credentials = new OAuthCredentials(credential.AccessToken),
            Timeout = 100000 // 100 seconds
        };

        _logger.LogInformation(
            "ExchangeCalendarEventRepository initialized for calendar {CalendarId} ({CalendarName})",
            calendar.Id,
            calendar.Name);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DomainCalendarEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all events for calendar {CalendarId}", _calendar.Id);

        return await ExecuteWithRetryAsync(async () =>
        {
            // Get events for a configurable time window
            var startDate = DateTime.Now.AddDays(-DefaultPastDaysToRetrieve);
            var endDate = DateTime.Now.AddDays(DefaultFutureDaysToRetrieve);

            var view = new CalendarView(startDate, endDate)
            {
                PropertySet = new PropertySet(BasePropertySet.IdOnly)
            };

            var folderId = GetCalendarFolderId();
            var appointments = await _service.FindAppointments(folderId, view);

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

        return await ExecuteWithRetryAsync(async () =>
        {
            // Create filters for extended properties
            var originalEventIdFilter = new SearchFilter.IsEqualTo(
                OriginalEventIdProperty,
                sourceEvent.ExternalId);

            var sourceCalendarIdFilter = new SearchFilter.IsEqualTo(
                SourceCalendarIdProperty,
                sourceCalendar.Id.ToString());

            // Combine filters with AND
            var combinedFilter = new SearchFilter.SearchFilterCollection(
                LogicalOperator.And,
                originalEventIdFilter,
                sourceCalendarIdFilter);

            // Create view with extended properties
            var view = new ItemView(1)
            {
                PropertySet = new PropertySet(
                    BasePropertySet.IdOnly,
                    OriginalEventIdProperty,
                    SourceCalendarIdProperty)
            };

            var folderId = GetCalendarFolderId();
            var results = await _service.FindItems(folderId, combinedFilter, view);

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
    public async System.Threading.Tasks.Task AddAsync(DomainCalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent, nameof(calendarEvent));

        _logger.LogInformation(
            "Adding calendar event '{Subject}' to calendar {CalendarId}",
            calendarEvent.Subject,
            _calendar.Id);

        await ExecuteWithRetryAsync(async () =>
        {
            var appointment = new Appointment(_service)
            {
                Subject = calendarEvent.Subject,
                Body = new MessageBody(
                    calendarEvent.BodyType == CalendarEventBodyType.Html ? BodyType.HTML : BodyType.Text,
                    calendarEvent.Body ?? string.Empty),
                Start = calendarEvent.Start,
                End = calendarEvent.End,
                Location = calendarEvent.Location ?? string.Empty,
                IsAllDayEvent = calendarEvent.IsAllDay
            };

            // Store metadata about copied events using extended properties
            if (calendarEvent.IsCopiedEvent && calendarEvent.OriginalEventId != null)
            {
                appointment.SetExtendedProperty(OriginalEventIdProperty, calendarEvent.OriginalEventId);

                if (calendarEvent.SourceCalendarId.HasValue)
                {
                    appointment.SetExtendedProperty(SourceCalendarIdProperty, calendarEvent.SourceCalendarId.Value.ToString());
                }
            }

            var folderId = GetCalendarFolderId();
            await appointment.Save(folderId);

            calendarEvent.ExternalId = appointment.Id.UniqueId;

            _logger.LogInformation(
                "Calendar event created with ID: {EventId}",
                appointment.Id.UniqueId);
        }, cancellationToken);
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
    /// Gets the folder ID for the calendar
    /// </summary>
    private FolderId GetCalendarFolderId()
    {
        return new FolderId(_calendar.ExternalId);
    }

    /// <summary>
    /// Maps Exchange Appointment to domain CalendarEvent
    /// </summary>
    private DomainCalendarEvent MapToCalendarEvent(Appointment appointment)
    {
        // Read metadata from extended properties
        var hasOriginalEventId = appointment.TryGetProperty(OriginalEventIdProperty, out string? originalEventId);
        var hasSourceCalendarId = appointment.TryGetProperty(SourceCalendarIdProperty, out string? sourceCalendarIdStr);

        Guid? sourceCalendarId = null;
        if (hasSourceCalendarId && !string.IsNullOrEmpty(sourceCalendarIdStr) && Guid.TryParse(sourceCalendarIdStr, out var parsedId))
        {
            sourceCalendarId = parsedId;
        }

        var isCopiedEvent = hasOriginalEventId && !string.IsNullOrEmpty(originalEventId);

        // Determine body type from Exchange appointment
        var bodyType = appointment.Body.BodyType == BodyType.HTML 
            ? CalendarEventBodyType.Html 
            : CalendarEventBodyType.Text;

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
            IsAllDay = appointment.IsAllDayEvent,
            IsRecurring = appointment.IsRecurring,
            Organizer = appointment.Organizer?.Address,
            CalendarId = _calendar.Id,
            OriginalEventId = isCopiedEvent ? originalEventId : null,
            SourceCalendarId = sourceCalendarId
        };
    }
}
