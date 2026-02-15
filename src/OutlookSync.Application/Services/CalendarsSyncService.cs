using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.Services;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Application.Services;

/// <summary>
/// Service for synchronizing multiple calendars by copying events between them
/// </summary>
public class CalendarsSyncService(
    ICalendarRepository calendarRepository,
    ICredentialRepository credentialRepository,
    ICalendarEventRepositoryFactory calendarEventRepositoryFactory,
    ILogger<CalendarsSyncService> logger) : ICalendarsSyncService
{
    private readonly ICalendarRepository _calendarRepository = calendarRepository;
    private readonly ICredentialRepository _credentialRepository = credentialRepository;
    private readonly ICalendarEventRepositoryFactory _calendarEventRepositoryFactory = calendarEventRepositoryFactory;
    private readonly ILogger<CalendarsSyncService> _logger = logger;

    private const string CopiedEventMarker = "[SYNCED]";

    public async Task<CalendarsSyncResult> SyncAllCalendarsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting synchronization of all calendars");

        var calendars = _calendarRepository.Query
            .Where(c => c.IsEnabled)
            .ToList();

        if (calendars.Count == 0)
        {
            _logger.LogWarning("No enabled calendars found for synchronization");
            return CalendarsSyncResult.Success(0, 0);
        }

        var successful = 0;
        var failed = 0;
        var totalEventsCopied = 0;
        var errors = new List<string>();

        foreach (var calendar in calendars)
        {
            try
            {
                // Get target calendars for this source calendar (all enabled calendars except the current one)
                var targetCalendars = calendars
                    .Where(c => c.Id != calendar.Id)
                    .ToList();

                var result = await SyncCalendarAsync(calendar, targetCalendars, cancellationToken);
                
                if (result.IsSuccess)
                {
                    successful++;
                    totalEventsCopied += result.ItemsSynced;
                }
                else
                {
                    failed++;
                    errors.Add($"Calendar {calendar.Name}: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"Calendar {calendar.Name}: {ex.Message}");
                _logger.LogError(ex, "Error syncing calendar {CalendarId}", calendar.Id);
            }
        }

        _logger.LogInformation(
            "Synchronization completed. Total: {Total}, Successful: {Successful}, Failed: {Failed}, Events copied: {EventsCopied}",
            calendars.Count, successful, failed, totalEventsCopied);

        return failed == 0
            ? CalendarsSyncResult.Success(calendars.Count, totalEventsCopied)
            : CalendarsSyncResult.Partial(calendars.Count, successful, failed, totalEventsCopied, errors);
    }

    private async Task<SyncResult> SyncCalendarAsync(
        Calendar sourceCalendar, 
        IReadOnlyList<Calendar> targetCalendars,
        CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.GetByIdAsync(sourceCalendar.CredentialId, cancellationToken);
        if (credential == null)
        {
            return SyncResult.Failure($"Credential not found for calendar {sourceCalendar.Name}");
        }

        if (!credential.IsTokenValid() || credential.StatusData == null || credential.StatusData.Length == 0)
        {
            return SyncResult.Failure($"Invalid token or missing status data for calendar {sourceCalendar.Name}");
        }

        try
        {
            if (targetCalendars.Count == 0)
            {
                _logger.LogInformation("No target calendars found for {CalendarName}", sourceCalendar.Name);
                return SyncResult.Success(0);
            }

            var totalEventsCopied = 0;

            // Create repository for this calendar with its credentials
            // The factory will validate the credential and token
            var sourceRepository = _calendarEventRepositoryFactory.Create(sourceCalendar, credential);
            
            // Initialize repository to verify connectivity and access
            await sourceRepository.InitAsync(cancellationToken);

            // Fetch events from local repository
            var sourceEvents = await sourceRepository.GetAllAsync(cancellationToken);

            // Filter out events that are already copied (avoid re-copying)
            var originalEvents = sourceEvents
                .Where(e => !e.IsCopiedEvent)
                .ToList();

            _logger.LogInformation(
                "Found {TotalEvents} events in {CalendarName}, {OriginalEvents} are original (not copied)",
                sourceEvents.Count, sourceCalendar.Name, originalEvents.Count);

            // Copy events to each target calendar
            foreach (var targetCalendar in targetCalendars)
            {
                try
                {
                    var targetCredential = await _credentialRepository.GetByIdAsync(targetCalendar.CredentialId, cancellationToken);
                    if (targetCredential == null)
                    {
                        _logger.LogWarning("Credential not found for target calendar {CalendarName}", targetCalendar.Name);
                        continue;
                    }

                    // The factory will validate the credential and token
                    var targetRepository = _calendarEventRepositoryFactory.Create(targetCalendar, targetCredential);
                    
                    // Initialize repository to verify connectivity and access
                    await targetRepository.InitAsync(cancellationToken);

                    totalEventsCopied += await CopyEventsToCalendarAsync(
                        originalEvents,
                        sourceCalendar,
                        targetCalendar,
                        targetRepository,
                        cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Skipping target calendar {CalendarName} due to invalid credentials", targetCalendar.Name);
                }
            }

            sourceCalendar.RecordSuccessfulSync(totalEventsCopied);
            await _calendarRepository.UpdateAsync(sourceCalendar, cancellationToken);

            _logger.LogInformation(
                "Successfully synced {EventCount} events from {CalendarName}",
                totalEventsCopied, sourceCalendar.Name);

            return SyncResult.Success(totalEventsCopied);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing calendar {CalendarId}", sourceCalendar.Id);
            sourceCalendar.RecordFailedSync(ex.Message);
            await _calendarRepository.UpdateAsync(sourceCalendar, cancellationToken);
            return SyncResult.Failure(ex.Message);
        }
    }

    private async Task<int> CopyEventsToCalendarAsync(
        IReadOnlyList<CalendarEvent> sourceEvents,
        Calendar sourceCalendar,
        Calendar targetCalendar,
        ICalendarEventRepository targetRepository,
        CancellationToken cancellationToken)
    {
        var eventsCopied = 0;

        foreach (var sourceEvent in sourceEvents)
        {
            try
            {
                // Check if we've already copied this event
                var existingCopy = await targetRepository.FindCopiedEventAsync(
                    sourceEvent,
                    sourceCalendar,
                    cancellationToken);

                if (existingCopy != null)
                {
                    _logger.LogDebug("Event {EventId} already copied to {TargetCalendar}", 
                        sourceEvent.ExternalId, targetCalendar.Name);
                    continue;
                }

                // Create copied event
                var copiedEvent = CreateCopiedEvent(sourceEvent, sourceCalendar, targetCalendar);

                // For now, just track in repository
                await targetRepository.AddAsync(copiedEvent, cancellationToken);

                eventsCopied++;

                _logger.LogDebug(
                    "Copied event {EventSubject} from {SourceCalendar} to {TargetCalendar}",
                    sourceEvent.Subject, sourceCalendar.Name, targetCalendar.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error copying event {EventId} from {SourceCalendar} to {TargetCalendar}",
                    sourceEvent.ExternalId, sourceCalendar.Name, targetCalendar.Name);
            }
        }

        return eventsCopied;
    }

    private static CalendarEvent CreateCopiedEvent(
        CalendarEvent sourceEvent,
        Calendar sourceCalendar,
        Calendar targetCalendar)
    {
        var config = targetCalendar.Configuration;
        var fieldSelection = config.FieldSelection;

        // Add marker to subject to indicate it's a copied event
        var subject = fieldSelection.Subject
            ? $"{CopiedEventMarker} {sourceEvent.Subject}"
            : $"{CopiedEventMarker} Event from {sourceCalendar.Name}";

        // Generate new external ID for the copy
        var newExternalId = $"copy_{Guid.NewGuid()}";

        return new CalendarEvent
        {
            Id = Guid.NewGuid(),
            CalendarId = targetCalendar.Id,
            ExternalId = newExternalId,
            Subject = subject,
            Start = fieldSelection.StartTime ? sourceEvent.Start : DateTime.UtcNow,
            End = fieldSelection.EndTime ? sourceEvent.End : DateTime.UtcNow.AddHours(1),
            Location = fieldSelection.Location ? sourceEvent.Location : null,
            Body = fieldSelection.Body ? sourceEvent.Body : null,
            Organizer = fieldSelection.Organizer ? sourceEvent.Organizer : null,
            IsAllDay = fieldSelection.IsAllDay && sourceEvent.IsAllDay,
            IsRecurring = fieldSelection.Recurrence && sourceEvent.IsRecurring,
            OriginalEventId = sourceEvent.ExternalId,
            SourceCalendarId = sourceCalendar.Id
        };
    }
}
