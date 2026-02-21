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
        Guid sourceCredentialId,
        string sourceExternalId,
        Guid targetCredentialId,
        string targetExternalId,
        Guid? excludeBindingId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<CalendarBinding>()
            .Where(cb => cb.SourceCredentialId == sourceCredentialId &&
                        cb.SourceCalendarExternalId == sourceExternalId &&
                        cb.TargetCredentialId == targetCredentialId &&
                        cb.TargetCalendarExternalId == targetExternalId);
        
        if (excludeBindingId.HasValue)
        {
            query = query.Where(cb => cb.Id != excludeBindingId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarBinding>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<CalendarBinding>()
            .Where(cb => cb.IsEnabled)
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarBinding>> GetBySourceAsync(
        Guid sourceCredentialId,
        string sourceExternalId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<CalendarBinding>()
            .Where(cb => cb.SourceCredentialId == sourceCredentialId &&
                        cb.SourceCalendarExternalId == sourceExternalId)
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarBinding>> GetByTargetAsync(
        Guid targetCredentialId,
        string targetExternalId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<CalendarBinding>()
            .Where(cb => cb.TargetCredentialId == targetCredentialId &&
                        cb.TargetCalendarExternalId == targetExternalId)
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarBinding>> GetByCredentialAndExternalIdAsync(
        Guid credentialId,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<CalendarBinding>()
            .Where(cb => (cb.SourceCredentialId == credentialId && cb.SourceCalendarExternalId == externalId) ||
                        (cb.TargetCredentialId == credentialId && cb.TargetCalendarExternalId == externalId))
            .ToListAsync(cancellationToken);
    }
}
