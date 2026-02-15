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
    IUnitOfWork unitOfWork,
    ILogger<CalendarsSyncService> logger) : ICalendarsSyncService
{
    private const string CopiedEventMarker = "[SYNCED]";

    public async Task<CalendarsSyncResult> SyncAllCalendarsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting synchronization of all calendars");

        var calendars = calendarRepository.Query
            .Where(c => c.IsEnabled)
            .ToList();

        if (calendars.Count == 0)
        {
            logger.LogWarning("No enabled calendars found for synchronization");
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
                logger.LogError(ex, "Error syncing calendar {CalendarId}", calendar.Id);
            }
        }

        // Save all changes at once
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("All changes saved successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save changes after sync");
            throw;
        }

        logger.LogInformation(
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
        var credential = await credentialRepository.GetByIdAsync(sourceCalendar.CredentialId, cancellationToken);
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
                logger.LogInformation("No target calendars found for {CalendarName}", sourceCalendar.Name);
                return SyncResult.Success(0);
            }

            var totalEventsCopied = 0;

            // Create repository for this calendar with its credentials
            // The factory will validate the credential and token
            var sourceRepository = calendarEventRepositoryFactory.Create(sourceCalendar, credential);
            
            // Initialize repository to verify connectivity and access
            await sourceRepository.InitAsync(cancellationToken);

            // Fetch events from local repository
            var sourceEvents = await sourceRepository.GetAllAsync(cancellationToken);

            // Filter out events that are already copied (avoid re-copying)
            var originalEvents = sourceEvents
                .Where(e => !e.IsCopiedEvent)
                .ToList();

            logger.LogInformation(
                "Found {TotalEvents} events in {CalendarName}, {OriginalEvents} are original (not copied)",
                sourceEvents.Count, sourceCalendar.Name, originalEvents.Count);

            // Copy events to each target calendar
            foreach (var targetCalendar in targetCalendars)
            {
                try
                {
                    var targetCredential = await credentialRepository.GetByIdAsync(targetCalendar.CredentialId, cancellationToken);
                    if (targetCredential == null)
                    {
                        logger.LogWarning("Credential not found for target calendar {CalendarName}", targetCalendar.Name);
                        continue;
                    }

                    // The factory will validate the credential and token
                    var targetRepository = calendarEventRepositoryFactory.Create(targetCalendar, targetCredential);
                    
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
                    logger.LogWarning(ex, "Skipping target calendar {CalendarName} due to invalid credentials", targetCalendar.Name);
                }
            }

            sourceCalendar.RecordSuccessfulSync(totalEventsCopied);
            await calendarRepository.UpdateAsync(sourceCalendar, cancellationToken);

            logger.LogInformation(
                "Successfully synced {EventCount} events from {CalendarName}",
                totalEventsCopied, sourceCalendar.Name);

            return SyncResult.Success(totalEventsCopied);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing calendar {CalendarId}", sourceCalendar.Id);
            sourceCalendar.RecordFailedSync(ex.Message);
            await calendarRepository.UpdateAsync(sourceCalendar, cancellationToken);
            return SyncResult.Failure(ex.Message);
        }
        finally
        {
            // Always update source credential as token cache may have been updated during sync
            try
            {
                await credentialRepository.UpdateAsync(credential, cancellationToken);
                logger.LogDebug("Credential updated for calendar {CalendarName}", sourceCalendar.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update credential for calendar {CalendarId}", sourceCalendar.Id);
            }
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
                    logger.LogDebug("Event {EventId} already copied to {TargetCalendar}", 
                        sourceEvent.ExternalId, targetCalendar.Name);
                    continue;
                }

                // Create copied event
                var copiedEvent = CreateCopiedEvent(sourceEvent, sourceCalendar, targetCalendar);

                // For now, just track in repository
                await targetRepository.AddAsync(copiedEvent, cancellationToken);

                eventsCopied++;

                logger.LogDebug(
                    "Copied event {EventSubject} from {SourceCalendar} to {TargetCalendar}",
                    sourceEvent.Subject, sourceCalendar.Name, targetCalendar.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(
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
