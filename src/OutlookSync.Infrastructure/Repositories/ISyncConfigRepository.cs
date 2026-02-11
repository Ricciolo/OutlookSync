using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Infrastructure.Repositories;

public interface ISyncConfigRepository : IRepository<SyncConfig>
{
    Task<SyncConfig?> GetByCalendarIdAsync(Guid calendarId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<SyncConfig>> GetEnabledConfigsAsync(CancellationToken cancellationToken = default);
}
