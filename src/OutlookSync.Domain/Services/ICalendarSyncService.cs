using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Domain.Services;

/// <summary>
/// Domain service for calendar synchronization
/// </summary>
public interface ICalendarSyncService
{
    Task<SyncResult> SyncCalendarAsync(Calendar calendar, Device device, CancellationToken cancellationToken = default);
}
