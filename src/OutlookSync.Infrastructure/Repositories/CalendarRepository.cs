using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Infrastructure.Persistence;

namespace OutlookSync.Infrastructure.Repositories;

public class CalendarRepository(OutlookSyncDbContext context) : Repository<Calendar>(context), ICalendarRepository
{
    public async Task<Calendar?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) =>
        await _context.Calendars
            .FirstOrDefaultAsync(c => c.ExternalId == externalId, cancellationToken);

    public async Task<IEnumerable<Calendar>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default) =>
        await _context.Calendars
            .Where(c => c.DeviceId == deviceId)
            .ToListAsync(cancellationToken);
}
