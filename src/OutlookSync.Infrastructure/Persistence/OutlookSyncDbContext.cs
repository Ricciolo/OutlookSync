using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Infrastructure.Persistence;

/// <summary>
/// Application DbContext for OutlookSync
/// </summary>
public class OutlookSyncDbContext: DbContext
{
    public OutlookSyncDbContext(DbContextOptions<OutlookSyncDbContext> options) : base(options)
    {
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public DbSet<Calendar> Calendars => Set<Calendar>();
    
    public DbSet<Credential> Credentials => Set<Credential>();
    
    public DbSet<CalendarBinding> CalendarBindings => Set<CalendarBinding>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutlookSyncDbContext).Assembly);
    }
}
