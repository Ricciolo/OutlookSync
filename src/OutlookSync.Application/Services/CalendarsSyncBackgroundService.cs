using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Services;

namespace OutlookSync.Application.Services;

/// <summary>
/// Background service for manual calendar synchronization triggering
/// </summary>
public class CalendarsSyncBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<CalendarsSyncBackgroundService> logger) : BackgroundService, ICalendarsSyncBackgroundService
{
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    
    private int _isSyncing;

    /// <inheritdoc />
    public bool IsSyncing => Interlocked.CompareExchange(ref _isSyncing, 0, 0) == 1;

    /// <inheritdoc />
    public async Task<bool> TriggerSyncAllAsync(CancellationToken cancellationToken = default)
    {
        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            logger.LogWarning("Cannot trigger sync: synchronization already in progress");
            return false;
        }

        try
        {
            Interlocked.Exchange(ref _isSyncing, 1);
            logger.LogInformation("Manual synchronization of all bindings triggered");
            
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ICalendarsSyncService>();
            await syncService.SyncAllCalendarsAsync(cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during manually triggered sync of all bindings");
            throw;
        }
        finally
        {
            Interlocked.Exchange(ref _isSyncing, 0);
            _syncLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> TriggerSyncBindingAsync(Guid bindingId, CancellationToken cancellationToken = default)
    {
        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            logger.LogWarning("Cannot trigger sync for binding {BindingId}: synchronization already in progress", bindingId);
            return false;
        }

        try
        {
            Interlocked.Exchange(ref _isSyncing, 1);
            logger.LogInformation("Manual synchronization of binding {BindingId} triggered", bindingId);
            
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ICalendarsSyncService>();
            await syncService.SyncCalendarBindingAsync(bindingId, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during manually triggered sync of binding {BindingId}", bindingId);
            throw;
        }
        finally
        {
            Interlocked.Exchange(ref _isSyncing, 0);
            _syncLock.Release();
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Calendar synchronization service started (manual trigger only)");
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _syncLock.Dispose();
        base.Dispose();
    }
}
