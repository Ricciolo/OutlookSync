using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Infrastructure.Persistence;

namespace OutlookSync.Infrastructure.Repositories;

public class SyncConfigRepository(OutlookSyncDbContext context) : Repository<SyncConfig>(context), ISyncConfigRepository
{
    public async Task<SyncConfig?> GetByCalendarIdAsync(Guid calendarId, CancellationToken cancellationToken = default) =>
        await _context.SyncConfigs
            .FirstOrDefaultAsync(sc => sc.CalendarId == calendarId, cancellationToken);

    public async Task<IEnumerable<SyncConfig>> GetEnabledConfigsAsync(CancellationToken cancellationToken = default) =>
        await _context.SyncConfigs
            .Where(sc => sc.IsEnabled)
            .ToListAsync(cancellationToken);
}
