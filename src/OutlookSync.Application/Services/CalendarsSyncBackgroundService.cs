using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.Services;

namespace OutlookSync.Application.Services;

/// <summary>
/// Background service for automatic and manual calendar synchronization
/// </summary>
public partial class CalendarsSyncBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<CalendarsSyncBackgroundService> logger) : BackgroundService, ICalendarsSyncBackgroundService
{
    private readonly ConcurrentDictionary<Guid, BindingSyncState> _bindingStates = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bindingSemaphores = new();
    private int _activeSyncCount;
    private CancellationTokenSource? _scheduleCancellation;

    /// <summary>
    /// Holds the sync state for a specific calendar binding
    /// </summary>
    private sealed class BindingSyncState
    {
        public CalendarBinding Binding { get; set; } = null!;
        public DateTime NextSyncAt { get; set; }
        public bool IsSyncing { get; set; }
        public Task? SyncTask { get; set; }
    }

    /// <inheritdoc />
    public bool IsSyncing => Interlocked.CompareExchange(ref _activeSyncCount, 0, 0) > 0;

    /// <inheritdoc />
    public bool IsAutoSyncEnabled => true;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScheduledBindingInfo>> GetScheduledBindingsAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<ScheduledBindingInfo>();
        
        foreach (var kvp in _bindingStates)
        {
            var state = kvp.Value;
            result.Add(new ScheduledBindingInfo
            {
                BindingId = kvp.Key,
                Name = state.Binding.Name,
                LastSyncAt = state.Binding.LastSyncAt,
                NextSyncAt = state.NextSyncAt,
                IsSyncing = state.IsSyncing,
                IntervalMinutes = state.Binding.Configuration.Interval.Minutes
            });
        }
        
        return result.OrderBy(x => x.NextSyncAt).ToList();
    }

    /// <inheritdoc />
    public async Task RescheduleAllAsync(CancellationToken cancellationToken = default)
    {
        LogReschedulingAll(logger);
        
        // Cancel all scheduled tasks
        _scheduleCancellation?.Cancel();
        _scheduleCancellation?.Dispose();
        _scheduleCancellation = new CancellationTokenSource();

        // Wait for existing tasks to complete
        var existingTasks = _bindingStates.Values
            .Where(s => s.SyncTask != null)
            .Select(s => s.SyncTask!)
            .ToList();
        
        if (existingTasks.Count > 0)
        {
            await Task.WhenAll(existingTasks);
        }
        
        // Reload and reschedule
        await LoadBindingsAsync(cancellationToken);
        ScheduleAllBindings();
    }

    /// <inheritdoc />
    public async Task<bool> TriggerSyncAllAsync(CancellationToken cancellationToken = default)
    {
        LogManualSyncAllTriggered(logger);
        
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var bindingRepository = scope.ServiceProvider.GetRequiredService<ICalendarBindingRepository>();
        var bindings = await bindingRepository.GetEnabledAsync(cancellationToken);
        
        var tasks = new List<Task<bool>>();
        foreach (var binding in bindings)
        {
            tasks.Add(TriggerSyncBindingAsync(binding.Id, cancellationToken));
        }
        
        var results = await Task.WhenAll(tasks);
        return results.Any(r => r);
    }

    /// <inheritdoc />
    public async Task<bool> TriggerSyncBindingAsync(Guid bindingId, CancellationToken cancellationToken = default)
    {
        LogManualSyncBindingTriggered(logger, bindingId);
        
        // Get or create semaphore for this binding
        var semaphore = _bindingSemaphores.GetOrAdd(bindingId, _ => new SemaphoreSlim(1, 1));
        
        // Try to acquire the semaphore without blocking
        if (!await semaphore.WaitAsync(0, cancellationToken))
        {
            LogAlreadySyncing(logger, bindingId);
            return false;
        }

        // Run and await the sync (semaphore will be released in SyncBindingInternalAsync)
        await SyncBindingInternalAsync(bindingId, cancellationToken);
        
        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStarted(logger);

        _scheduleCancellation = new CancellationTokenSource();

        // Initial load of bindings
        await LoadBindingsAsync(stoppingToken);
        
        // Schedule all bindings
        ScheduleAllBindings();
        
        // Wait until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogServiceStopping(logger);
        }

        // Cancel all scheduled tasks
        _scheduleCancellation?.Cancel();

        // Wait for all active syncs to complete before shutting down
        await WaitForActiveSyncsAsync();
    }

    private async Task LoadBindingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var bindingRepository = scope.ServiceProvider.GetRequiredService<ICalendarBindingRepository>();
            var bindings = await bindingRepository.GetEnabledAsync(cancellationToken);
            
            LogBindingsLoaded(logger, bindings.Count);
            
            _bindingStates.Clear();
            
            foreach (var binding in bindings)
            {
                var nextSyncAt = CalculateNextSyncTime(binding);
                _bindingStates[binding.Id] = new BindingSyncState
                {
                    Binding = binding,
                    NextSyncAt = nextSyncAt,
                    IsSyncing = false
                };
                
                LogBindingScheduled(logger, binding.Id, binding.Name, nextSyncAt);
            }
        }
        catch (Exception ex)
        {
            LogErrorLoadingBindings(logger, ex);
        }
    }

    private void ScheduleAllBindings()
    {
        foreach (var kvp in _bindingStates)
        {
            ScheduleBinding(kvp.Key, kvp.Value);
        }
    }

    private void ScheduleBinding(Guid bindingId, BindingSyncState state)
    {
        var cancellationToken = _scheduleCancellation?.Token ?? CancellationToken.None;
        
        state.SyncTask = Task.Run(async () =>
        {
            LogSchedulingLoopStarted(logger, bindingId, state.Binding.Name);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Calculate delay until next sync
                    var delay = state.NextSyncAt - DateTime.UtcNow;
                    if (delay > TimeSpan.Zero)
                    {
                        LogBindingWillSync(logger, bindingId, state.Binding.Name, delay);

                        await Task.Delay(delay, cancellationToken);
                    }
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Trigger sync (NextSyncAt is updated inside SyncBindingInternalAsync)
                    await TriggerSyncBindingAsync(bindingId, cancellationToken);
                    
                    // Check if binding was disabled during sync
                    if (!state.Binding.IsEnabled)
                    {
                        LogBindingDisabled(logger, bindingId);
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    LogSchedulingLoopCancelled(logger, bindingId);
                    break;
                }
                catch (Exception ex)
                {
                    LogErrorInSchedulingLoop(logger, ex, bindingId);
                    
                    // Wait a bit before retrying to avoid tight error loop
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
            }
            
            LogSchedulingLoopEnded(logger, bindingId);
        }, cancellationToken);
    }

    private async Task SyncBindingInternalAsync(Guid bindingId, CancellationToken cancellationToken)
    {
        // Get the semaphore for this binding (must exist since it was created before calling this method)
        var semaphore = _bindingSemaphores.GetOrAdd(bindingId, _ => new SemaphoreSlim(1, 1));
        
        if (!_bindingStates.TryGetValue(bindingId, out var state))
        {
            LogBindingNotFound(logger, bindingId);
            semaphore.Release();
            return;
        }

        state.IsSyncing = true;
        Interlocked.Increment(ref _activeSyncCount);

        try
        {
            LogSyncStarted(logger, bindingId, state.Binding.Name);

            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ICalendarsSyncService>();
            var result = await syncService.SyncCalendarBindingAsync(bindingId, cancellationToken);

            if (result.IsSuccess)
            {
                LogSyncCompleted(logger, bindingId, state.Binding.Name, result.ItemsSynced);
            }
            else
            {
                LogSyncFailed(logger, bindingId, state.Binding.Name, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            LogSyncError(logger, ex, bindingId, state.Binding.Name);
        }
        finally
        {
            state.IsSyncing = false;
            Interlocked.Decrement(ref _activeSyncCount);
            semaphore.Release();
        }

        // Reload binding from DB to get updated LastSyncAt and recalculate next sync time
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var bindingRepository = scope.ServiceProvider.GetRequiredService<ICalendarBindingRepository>();
            var updatedBinding = await bindingRepository.GetByIdAsync(bindingId, cancellationToken);

            if (updatedBinding != null)
            {
                state.Binding = updatedBinding;
                state.NextSyncAt = CalculateNextSyncTime(updatedBinding);

                LogNextSyncScheduled(logger, bindingId, state.Binding.Name, state.NextSyncAt);
            }
        }
        catch (Exception ex)
        {
            LogErrorReloadingBinding(logger, ex, bindingId);
        }
    }

    private static DateTime CalculateNextSyncTime(CalendarBinding binding)
    {
        var lastSync = binding.LastSyncAt ?? DateTime.UtcNow.AddMinutes(-binding.Configuration.Interval.Minutes);
        return lastSync.AddMinutes(binding.Configuration.Interval.Minutes);
    }

    private async Task WaitForActiveSyncsAsync()
    {
        var activeTasks = _bindingStates.Values
            .Where(s => s.SyncTask != null && !s.SyncTask.IsCompleted)
            .Select(s => s.SyncTask!)
            .ToList();

        if (activeTasks.Count > 0)
        {
            LogWaitingForActiveSyncs(logger, activeTasks.Count);
            await Task.WhenAll(activeTasks);
        }
    }

    public override void Dispose()
    {
        // Cancel all scheduled tasks
        _scheduleCancellation?.Cancel();
        _scheduleCancellation?.Dispose();
        
        // Dispose all binding semaphores
        foreach (var semaphore in _bindingSemaphores.Values)
        {
            semaphore.Dispose();
        }

        _bindingSemaphores.Clear();
        
        GC.SuppressFinalize(this);
        base.Dispose();
    }
}
