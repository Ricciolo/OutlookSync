using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Infrastructure.Repositories;

public interface ICalendarRepository : IRepository<Calendar>
{
    Task<Calendar?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Calendar>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default);
}
