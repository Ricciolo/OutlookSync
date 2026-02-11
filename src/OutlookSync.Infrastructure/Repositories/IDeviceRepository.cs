using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Infrastructure.Repositories;

public interface IDeviceRepository : IRepository<Device>
{
    Task<IEnumerable<Device>> GetDevicesWithValidTokensAsync(CancellationToken cancellationToken = default);
}
