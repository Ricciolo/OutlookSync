using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OutlookSync.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating DbContext instances during migrations
/// </summary>
public class OutlookSyncDbContextFactory : IDesignTimeDbContextFactory<OutlookSyncDbContext>
{
    public OutlookSyncDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OutlookSyncDbContext>();
        optionsBuilder.UseSqlite("Data Source=outlooksync.db");

        return new OutlookSyncDbContext(optionsBuilder.Options);
    }
}
