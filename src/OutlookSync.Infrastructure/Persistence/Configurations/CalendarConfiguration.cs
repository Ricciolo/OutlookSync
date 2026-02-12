using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Infrastructure.Persistence.Configurations;

public class CalendarConfiguration : IEntityTypeConfiguration<Calendar>
{
    public void Configure(EntityTypeBuilder<Calendar> builder)
    {
        builder.ToTable("Calendars");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        
        builder.Property(c => c.ExternalId).HasMaxLength(200).IsRequired();
        
        builder.HasIndex(c => c.ExternalId).IsUnique();
        
        builder.Property(c => c.CredentialId).IsRequired();
        
        builder.Property(c => c.IsEnabled).IsRequired();
        
        builder.Property(c => c.SyncDaysForward).IsRequired();
        
        builder.OwnsOne(c => c.Configuration, config =>
        {
            config.OwnsOne(cfg => cfg.Interval, interval =>
            {
                interval.Property(i => i.Minutes).IsRequired();
                interval.Property(i => i.CronExpression).HasMaxLength(50);
            });
            
            config.Property(cfg => cfg.StartDate).IsRequired();
            config.Property(cfg => cfg.IsPrivate).IsRequired();
            
            config.OwnsOne(cfg => cfg.FieldSelection, fields =>
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
        
        builder.Property(c => c.LastSyncAt);
        
        builder.Property(c => c.CreatedAt).IsRequired();
        
        builder.Property(c => c.UpdatedAt);
    }
}
