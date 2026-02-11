using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Infrastructure.Persistence.Configurations;

public class SyncConfigConfiguration : IEntityTypeConfiguration<SyncConfig>
{
    public void Configure(EntityTypeBuilder<SyncConfig> builder)
    {
        builder.ToTable("SyncConfigs");
        
        builder.HasKey(sc => sc.Id);
        
        builder.Property(sc => sc.CalendarId).IsRequired();
        
        builder.HasIndex(sc => sc.CalendarId).IsUnique();
        
        builder.OwnsOne(sc => sc.Configuration, config =>
        {
            config.Property(c => c.IsPrivate).IsRequired();
            config.Property(c => c.StartDate).IsRequired();
            
            config.OwnsOne(c => c.Interval, interval =>
            {
                interval.Property(i => i.Minutes).IsRequired();
                interval.Property(i => i.CronExpression).HasMaxLength(50);
            });
            
            config.OwnsOne(c => c.FieldSelection, fields =>
            {
                fields.Property(f => f.Subject).IsRequired();
                fields.Property(f => f.StartTime).IsRequired();
                fields.Property(f => f.EndTime).IsRequired();
                fields.Property(f => f.Location).IsRequired();
                fields.Property(f => f.Attendees).IsRequired();
                fields.Property(f => f.Body).IsRequired();
                fields.Property(f => f.Organizer).IsRequired();
                fields.Property(f => f.IsAllDay).IsRequired();
                fields.Property(f => f.Recurrence).IsRequired();
            });
        });
        
        builder.Property(sc => sc.IsEnabled).IsRequired();
        
        builder.Property(sc => sc.LastSyncAt);
        
        builder.Property(sc => sc.LastSyncStatus).HasMaxLength(500);
        
        builder.Property(sc => sc.CreatedAt).IsRequired();
        
        builder.Property(sc => sc.UpdatedAt);
    }
}
