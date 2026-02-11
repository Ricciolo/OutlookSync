using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Infrastructure.Persistence;

namespace OutlookSync.Infrastructure.Repositories;

public class DeviceRepository(OutlookSyncDbContext context) : Repository<Device>(context), IDeviceRepository
{
    public async Task<IEnumerable<Device>> GetDevicesWithValidTokensAsync(CancellationToken cancellationToken = default) =>
        await _context.Devices
            .Where(d => d.TokenStatus == TokenStatus.Valid)
            .ToListAsync(cancellationToken);
}
