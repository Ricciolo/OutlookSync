using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Infrastructure.Persistence;

/// <summary>
/// Application DbContext for OutlookSync
/// </summary>
public class OutlookSyncDbContext(DbContextOptions<OutlookSyncDbContext> options) : DbContext(options)
{
    public DbSet<Calendar> Calendars => Set<Calendar>();
    
    public DbSet<Credential> Credentials => Set<Credential>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutlookSyncDbContext).Assembly);
    }
}
