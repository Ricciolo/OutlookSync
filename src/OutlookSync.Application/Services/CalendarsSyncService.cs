using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.Services;

namespace OutlookSync.Application.Services;

/// <summary>
/// Service for synchronizing multiple calendars by copying events between them using CalendarBindings
/// </summary>
public class CalendarsSyncService(
    ICalendarBindingRepository calendarBindingRepository,
    ICredentialRepository credentialRepository,
    ICalendarEventRepositoryFactory calendarEventRepositoryFactory,
    IUnitOfWork unitOfWork,
    ILogger<CalendarsSyncService> logger) : ICalendarsSyncService
{
    public async Task<CalendarsSyncResult> SyncAllCalendarsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting synchronization using calendar bindings");

        var bindings = await calendarBindingRepository.GetEnabledAsync(cancellationToken);

        if (bindings.Count == 0)
        {
            logger.LogWarning("No enabled calendar bindings found for synchronization");
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
                var result = await SyncBindingAsync(binding, cancellationToken);
                
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
                logger.LogError(ex, "Error syncing binding {BindingId}", binding.Id);
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
            bindings.Count, successful, failed, totalEventsCopied);

        return failed == 0
            ? CalendarsSyncResult.Success(bindings.Count, totalEventsCopied)
            : CalendarsSyncResult.Partial(bindings.Count, successful, failed, totalEventsCopied, errors);
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
            // Create repositories for source and target using external IDs
            var sourceRepository = calendarEventRepositoryFactory.Create(
                sourceCredential, 
                binding.SourceCalendarExternalId, 
                $"Source: {binding.Name}");
            await sourceRepository.InitAsync(cancellationToken);

            var targetRepository = calendarEventRepositoryFactory.Create(
                targetCredential, 
                binding.TargetCalendarExternalId, 
                $"Target: {binding.Name}");
            await targetRepository.InitAsync(cancellationToken);

            // Fetch events from source
            var sourceEvents = await sourceRepository.GetAllAsync(cancellationToken);

            // Filter out already-copied events
            var originalEvents = sourceEvents
                .Where(e => !e.IsCopiedEvent)
                .ToList();

            logger.LogInformation(
                "Found {TotalEvents} events in source calendar '{SourceName}', {OriginalEvents} are original (not copied)",
                sourceEvents.Count, binding.Name, originalEvents.Count);

            // Filter events based on binding exclusion rules
            var eventsToSync = originalEvents
                .Where(e => EventFilteringService.ShouldSyncEvent(e, binding.Configuration))
                .ToList();

            logger.LogInformation(
                "After filtering, {EventsToSync} events will be synchronized for binding {BindingName}",
                eventsToSync.Count, binding.Name);

            var eventsCopied = 0;

            foreach (var sourceEvent in eventsToSync)
            {
                try
                {
                    // Check if we've already copied this event
                    // Note: FindCopiedEventAsync may need to be updated to work without Calendar parameter
                    // For now, we'll skip the duplicate check and let Exchange handle it
                    
                    // Transform event based on binding configuration
                    var newExternalId = $"copy_{Guid.NewGuid()}";
                    var transformedEvent = EventFilteringService.TransformEvent(
                        sourceEvent,
                        binding,
                        binding.Name,
                        newExternalId);

                    // Add to target repository
                    await targetRepository.AddAsync(transformedEvent, cancellationToken);

                    eventsCopied++;

                    logger.LogDebug(
                        "Copied event {EventSubject} for binding {BindingName}",
                        sourceEvent.Subject, binding.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error copying event {EventId} for binding {BindingName}",
                        sourceEvent.ExternalId, binding.Name);
                }
            }

            binding.RecordSuccessfulSync(eventsCopied);
            await calendarBindingRepository.UpdateAsync(binding, cancellationToken);

            logger.LogInformation(
                "Successfully synced {EventCount} events for binding {BindingName}",
                eventsCopied, binding.Name);

            return SyncResult.Success(eventsCopied);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing binding {BindingId}", binding.Id);
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
                logger.LogDebug("Credentials updated for binding {BindingName}", binding.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update credentials for binding {BindingId}", binding.Id);
            }
        }
    }
}
