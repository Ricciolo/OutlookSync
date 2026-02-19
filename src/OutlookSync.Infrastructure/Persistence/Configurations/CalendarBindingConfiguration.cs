using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for CalendarBinding aggregate
/// </summary>
public class CalendarBindingConfiguration : IEntityTypeConfiguration<CalendarBinding>
{
    public void Configure(EntityTypeBuilder<CalendarBinding> builder)
    {
        builder.ToTable("CalendarBindings");
        
        builder.HasKey(cb => cb.Id);
        
        builder.Property(cb => cb.Name).HasMaxLength(200).IsRequired();
        
        builder.Property(cb => cb.SourceCredentialId).IsRequired();
        
        builder.Property(cb => cb.SourceCalendarExternalId).HasMaxLength(500).IsRequired();
        
        builder.Property(cb => cb.TargetCredentialId).IsRequired();
        
        builder.Property(cb => cb.TargetCalendarExternalId).HasMaxLength(500).IsRequired();
        
        // Unique constraint: one source-target pair per binding
        builder.HasIndex(cb => new { cb.SourceCredentialId, cb.SourceCalendarExternalId, cb.TargetCredentialId, cb.TargetCalendarExternalId })
            .IsUnique()
            .HasDatabaseName("IX_CalendarBindings_SourceTarget_Unique");
        
        builder.Property(cb => cb.IsEnabled).IsRequired();
        
        builder.Property(cb => cb.LastSyncAt);
        
        builder.Property(cb => cb.LastSyncEventCount).IsRequired().HasDefaultValue(0);
        
        builder.Property(cb => cb.LastSyncError).HasMaxLength(1000);
        
        // Configure the CalendarBindingConfiguration value object as owned
        builder.OwnsOne(cb => cb.Configuration, config =>
        {
            config.Property(c => c.TitleHandling)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            
            config.Property(c => c.CustomTitle).HasMaxLength(500);
            
            config.Property(c => c.CopyDescription).IsRequired();
            
            config.Property(c => c.CopyParticipants).IsRequired();
            
            config.Property(c => c.CopyLocation).IsRequired();
            
            config.Property(c => c.TargetCategory).HasMaxLength(200);
            
            config.Property(c => c.TargetStatus)
                .HasConversion<string>()
                .HasMaxLength(50);
            
            config.Property(c => c.CopyAttachments).IsRequired();
            
            config.Property(c => c.ReminderHandling)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            
            config.Property(c => c.MarkAsPrivate).IsRequired();
            
            config.Property(c => c.CustomTag).HasMaxLength(200);
            
            config.Property(c => c.CustomTagInTitle).IsRequired();
            
            // Configure RsvpExclusionRule as owned
            config.OwnsOne(c => c.RsvpExclusion, rsvpRule =>
            {
                rsvpRule.Property(rr => rr.ExcludedResponses)
                    .HasMaxLength(500)
                    .HasColumnName("ExcludedRsvpResponses")
                    .HasConversion(
                        v => string.Join(",", v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => Enum.Parse<RsvpResponse>(s.Trim()))
                            .ToArray()
                    );
            });
            
            // Configure StatusExclusionRule as owned
            config.OwnsOne(c => c.StatusExclusion, statusRule =>
            {
                statusRule.Property(sr => sr.ExcludedStatuses)
                    .HasMaxLength(500)
                    .HasColumnName("ExcludedStatuses")
                    .HasConversion(
                        v => string.Join(",", v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => Enum.Parse<EventStatus>(s.Trim()))
                            .ToArray()
                    );
            });
            
            // Configure SyncInterval as owned
            config.OwnsOne(c => c.Interval, interval =>
            {
                interval.Property(i => i.Minutes)
                    .IsRequired()
                    .HasColumnName("SyncIntervalMinutes");
                
                interval.Property(i => i.CronExpression)
                    .HasMaxLength(100)
                    .HasColumnName("SyncCronExpression");
            });
            
            config.Property(c => c.SyncDaysForward)
                .IsRequired()
                .HasDefaultValue(30);
        });
        
        builder.Property(cb => cb.CreatedAt).IsRequired();
        
        builder.Property(cb => cb.UpdatedAt);
    }
}
