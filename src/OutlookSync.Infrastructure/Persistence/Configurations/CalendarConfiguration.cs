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
        
        builder.Property(c => c.DeviceId).IsRequired();
        
        builder.Property(c => c.Owner).HasMaxLength(200);
        
        builder.Property(c => c.IsEnabled).IsRequired();
        
        builder.Property(c => c.TotalItemsSynced).IsRequired();
        
        builder.Property(c => c.LastSyncAt);
        
        builder.Property(c => c.CreatedAt).IsRequired();
        
        builder.Property(c => c.UpdatedAt);
        
        builder.Ignore(c => c.DomainEvents);
    }
}
