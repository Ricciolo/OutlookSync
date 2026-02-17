using OutlookSync.Domain.Aggregates;
using OutlookSync.Infrastructure.Persistence;

namespace OutlookSync.Infrastructure.Repositories;

/// <summary>
/// Calendar repository implementation.
/// </summary>
public class CalendarRepository(OutlookSyncDbContext context) 
    : Repository<Calendar>(context), ICalendarRepository
{
}
