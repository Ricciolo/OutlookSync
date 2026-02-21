using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.Services;

namespace OutlookSync.Application.Services;

/// <summary>
/// Service for synchronizing multiple calendars by copying events between them using CalendarBindings
/// </summary>
public partial class CalendarsSyncService(
    ICalendarBindingRepository calendarBindingRepository,
    ICredentialRepository credentialRepository,
    ICalendarEventRepositoryFactory calendarEventRepositoryFactory,
    IUnitOfWork unitOfWork,
    ILogger<CalendarsSyncService> logger) : ICalendarsSyncService
{
    public async Task<CalendarsSyncResult> SyncAllCalendarsAsync(CancellationToken cancellationToken = default)
    {
        LogStartingSyncAll(logger);

        var bindings = await calendarBindingRepository.GetEnabledAsync(cancellationToken);

        if (bindings.Count == 0)
        {
            LogNoBindingsFound(logger);
            return CalendarsSyncResult.Success(0, 0);
        }

        var successful = 0;
        var failed = 0;
        var totalEventsCopied = 0;
        var errors = new List<string>();

        foreach (var binding in bindings)
        {
            try
            {
                var result = await SyncCalendarBindingAsync(binding.Id, cancellationToken);
                
                if (result.IsSuccess)
                {
                    successful++;
                    totalEventsCopied += result.ItemsSynced;
                }
                else
                {
                    failed++;
                    errors.Add($"Binding {binding.Name}: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"Binding {binding.Name}: {ex.Message}");
                LogErrorSyncingBinding(logger, ex, binding.Id);
            }
        }

        LogSyncAllCompleted(logger, bindings.Count, successful, failed, totalEventsCopied);

        return failed == 0
            ? CalendarsSyncResult.Success(bindings.Count, totalEventsCopied)
            : CalendarsSyncResult.Partial(bindings.Count, successful, failed, totalEventsCopied, errors);
    }

    public async Task<SyncResult> SyncCalendarBindingAsync(Guid bindingId, CancellationToken cancellationToken = default)
    {
        LogStartingSyncBinding(logger, bindingId);

        var binding = await calendarBindingRepository.GetByIdAsync(bindingId, cancellationToken);
        
        if (binding == null)
        {
            LogBindingNotFound(logger, bindingId);
            return SyncResult.Failure("Calendar binding not found");
        }

        if (!binding.IsEnabled)
        {
            LogBindingDisabled(logger, bindingId);
            return SyncResult.Failure("Calendar binding is disabled");
        }

        var result = await SyncBindingAsync(binding, cancellationToken);

        // Save changes for this specific binding
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            LogChangesSaved(logger, bindingId);
        }
        catch (Exception ex)
        {
            LogErrorSavingChanges(logger, ex, bindingId);
            throw;
        }

        return result;
    }

    private async Task<SyncResult> SyncBindingAsync(
        CalendarBinding binding,
        CancellationToken cancellationToken)
    {
        // Get source and target credentials
        var sourceCredential = await credentialRepository.GetByIdAsync(binding.SourceCredentialId, cancellationToken);
        if (sourceCredential == null)
        {
            return SyncResult.Failure($"Source credential not found for binding {binding.Name}");
        }

        if (!sourceCredential.IsTokenValid() || sourceCredential.StatusData == null || sourceCredential.StatusData.Length == 0)
        {
            return SyncResult.Failure($"Invalid token or missing status data for source credential in binding {binding.Name}");
        }

        var targetCredential = await credentialRepository.GetByIdAsync(binding.TargetCredentialId, cancellationToken);
        if (targetCredential == null)
        {
            return SyncResult.Failure($"Target credential not found for binding {binding.Name}");
        }

        if (!targetCredential.IsTokenValid() || targetCredential.StatusData == null || targetCredential.StatusData.Length == 0)
        {
            return SyncResult.Failure($"Invalid token or missing status data for target credential in binding {binding.Name}");
        }

        try
        {
            // Create repositories for source and target
            var sourceRepository = calendarEventRepositoryFactory.Create(sourceCredential);
            await sourceRepository.InitAsync(cancellationToken);

            var targetRepository = calendarEventRepositoryFactory.Create(targetCredential);
            await targetRepository.InitAsync(cancellationToken);

            // Fetch events from source
            var sourceEvents = await sourceRepository.GetAllAsync(
                binding.SourceCalendarExternalId, 
                cancellationToken);

            // Enrich events with binding ID for tracking
            sourceEvents = sourceEvents
                .Select(e => e with { SourceCalendarBindingId = binding.Id })
                .ToList();

            // Filter out already-copied events
            var originalEvents = sourceEvents
                .Where(e => !e.IsCopiedEvent)
                .ToList();

            LogEventsFound(logger, sourceEvents.Count, binding.Name, originalEvents.Count);

            // Filter events based on binding exclusion rules
            var eventsToSync = originalEvents
                .Where(e => EventFilteringService.ShouldSyncEvent(e, binding.Configuration))
                .ToList();

            LogEventsAfterFiltering(logger, eventsToSync.Count, binding.Name);

            var eventsSynced = 0;

            foreach (var sourceEvent in eventsToSync)
            {
                try
                {
                    // Check if we've already copied this event
                    var existingCopy = await targetRepository.FindCopiedEventAsync(
                        sourceEvent.ExternalId,
                        binding.Id,
                        binding.TargetCalendarExternalId,
                        cancellationToken);

                    if (existingCopy != null)
                    {
                        // Event already exists - check if it needs updating
                        if (HasEventChanged(sourceEvent, existingCopy))
                        {
                            LogEventChanged(logger, sourceEvent.Subject);

                            // Transform event with existing ExternalId
                            var updatedEvent = EventFilteringService.TransformEvent(
                                sourceEvent,
                                binding,
                                binding.Name,
                                existingCopy.ExternalId);

                            // Update the existing event
                            await targetRepository.UpdateAsync(updatedEvent, binding.TargetCalendarExternalId, cancellationToken);
                            eventsSynced++;

                            LogEventUpdated(logger, sourceEvent.Subject, binding.Name);
                        }
                        else
                        {
                            LogEventUnchanged(logger, sourceEvent.Subject);
                        }

                        continue;
                    }

                    // Transform event based on binding configuration
                    var newExternalId = $"copy_{Guid.NewGuid()}";
                    var transformedEvent = EventFilteringService.TransformEvent(
                        sourceEvent,
                        binding,
                        binding.Name,
                        newExternalId);

                    // Add to target repository
                    await targetRepository.AddAsync(transformedEvent, binding.TargetCalendarExternalId, cancellationToken);

                    eventsSynced++;

                    LogEventCopied(logger, sourceEvent.Subject, binding.Name);
                }
                catch (Exception ex)
                {
                    LogErrorSyncingEvent(logger, ex, sourceEvent.ExternalId, binding.Name);
                }
            }

            // Delete events that no longer exist in source or are no longer being synced
            var sourceEventIds = originalEvents
                .Select(e => e.ExternalId)
                .ToHashSet();

            var copiedEvents = await targetRepository.GetCopiedEventsAsync(
                binding.Id,
                binding.TargetCalendarExternalId,
                cancellationToken);

            LogCopiedEventsFound(logger, copiedEvents.Count, binding.Name);

            foreach (var copiedEvent in copiedEvents)
            {
                try
                {
                    // If the original event no longer exists in source, delete the copy
                    if (copiedEvent.OriginalEventId == null || !sourceEventIds.Contains(copiedEvent.OriginalEventId))
                    {
                        LogDeletingOrphanedEvent(logger, copiedEvent.Subject, copiedEvent.OriginalEventId);

                        var deleted = await targetRepository.DeleteAsync(
                            copiedEvent.ExternalId,
                            binding.TargetCalendarExternalId,
                            cancellationToken);

                        if (deleted)
                        {
                            eventsSynced++;
                            LogOrphanedEventDeleted(logger, copiedEvent.Subject);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogErrorDeletingEvent(logger, ex, copiedEvent.ExternalId, binding.Name);
                }
            }

            binding.RecordSuccessfulSync(eventsToSync.Count);
            await calendarBindingRepository.UpdateAsync(binding, cancellationToken);

            LogSyncSuccess(logger, eventsToSync.Count, binding.Name, eventsSynced);

            return SyncResult.Success(eventsSynced);
        }
        catch (Exception ex)
        {
            LogErrorSyncingBinding(logger, ex, binding.Id);
            binding.RecordFailedSync(ex.Message);
            await calendarBindingRepository.UpdateAsync(binding, cancellationToken);
            return SyncResult.Failure(ex.Message);
        }
        finally
        {
            // Always update credentials as token cache may have been updated during sync
            try
            {
                await credentialRepository.UpdateAsync(sourceCredential, cancellationToken);
                await credentialRepository.UpdateAsync(targetCredential, cancellationToken);
                LogCredentialsUpdated(logger, binding.Name);
            }
            catch (Exception ex)
            {
                LogErrorUpdatingCredentials(logger, ex, binding.Id);
            }
        }
    }

    /// <summary>
    /// Determines if an event has changed by comparing relevant properties
    /// </summary>
    private static bool HasEventChanged(Domain.ValueObjects.CalendarEvent sourceEvent, Domain.ValueObjects.CalendarEvent existingEvent)
    {
        // Compare all relevant properties that should trigger an update
        return sourceEvent.Subject != existingEvent.Subject ||
               sourceEvent.Start != existingEvent.Start ||
               sourceEvent.End != existingEvent.End ||
               sourceEvent.Location != existingEvent.Location ||
               sourceEvent.IsOnlineMeeting != existingEvent.IsOnlineMeeting ||
               sourceEvent.IsMeeting != existingEvent.IsMeeting ||
               sourceEvent.Body != existingEvent.Body ||
               sourceEvent.BodyType != existingEvent.BodyType ||
               sourceEvent.Organizer != existingEvent.Organizer ||
               sourceEvent.IsAllDay != existingEvent.IsAllDay ||
               sourceEvent.IsRecurring != existingEvent.IsRecurring ||
               sourceEvent.Status != existingEvent.Status ||
               sourceEvent.RsvpStatus != existingEvent.RsvpStatus ||
               sourceEvent.IsPrivate != existingEvent.IsPrivate ||
               sourceEvent.Categories != existingEvent.Categories ||
               !AreAttendeesEqual(sourceEvent.RequiredAttendees, existingEvent.RequiredAttendees) ||
               !AreAttendeesEqual(sourceEvent.OptionalAttendees, existingEvent.OptionalAttendees) ||
               sourceEvent.HasAttachments != existingEvent.HasAttachments ||
               sourceEvent.ReminderMinutesBeforeStart != existingEvent.ReminderMinutesBeforeStart;
    }

    private static bool AreAttendeesEqual(IReadOnlyList<string> list1, IReadOnlyList<string> list2)
    {
        if (list1.Count != list2.Count)
        {
            return false;
        }
        
        return list1.SequenceEqual(list2);
    }
}
