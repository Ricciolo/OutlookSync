using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Infrastructure.Persistence;

namespace OutlookSync.Infrastructure.Repositories;

/// <summary>
/// CalendarBinding repository implementation
/// </summary>
public class CalendarBindingRepository(OutlookSyncDbContext context)
    : Repository<CalendarBinding>(context), ICalendarBindingRepository
{
    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        Guid sourceCalendarId,
        Guid targetCalendarId,
        Guid? excludeBindingId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<CalendarBinding>()
            .Where(cb => cb.SourceCalendarId == sourceCalendarId &&
                        cb.TargetCalendarId == targetCalendarId);
        
        if (excludeBindingId.HasValue)
        {
            query = query.Where(cb => cb.Id != excludeBindingId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarBinding>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<CalendarBinding>()
            .Where(cb => cb.IsEnabled)
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarBinding>> GetBySourceCalendarAsync(
        Guid sourceCalendarId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<CalendarBinding>()
            .Where(cb => cb.SourceCalendarId == sourceCalendarId)
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarBinding>> GetByTargetCalendarAsync(
        Guid targetCalendarId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<CalendarBinding>()
            .Where(cb => cb.TargetCalendarId == targetCalendarId)
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarBinding>> GetByCalendarAsync(
        Guid calendarId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<CalendarBinding>()
            .Where(cb => cb.SourceCalendarId == calendarId || cb.TargetCalendarId == calendarId)
            .ToListAsync(cancellationToken);
    }
}
