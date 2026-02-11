using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Infrastructure.Persistence;

/// <summary>
/// Application DbContext for OutlookSync
/// </summary>
public class OutlookSyncDbContext(DbContextOptions<OutlookSyncDbContext> options) : DbContext(options)
{
    public DbSet<Calendar> Calendars => Set<Calendar>();
    
    public DbSet<Device> Devices => Set<Device>();
    
    public DbSet<SyncConfig> SyncConfigs => Set<SyncConfig>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutlookSyncDbContext).Assembly);
    }
}
