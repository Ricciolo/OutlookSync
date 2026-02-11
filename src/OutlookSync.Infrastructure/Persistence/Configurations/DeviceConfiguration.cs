using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Infrastructure.Persistence.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");
        
        builder.HasKey(d => d.Id);
        
        builder.OwnsOne(d => d.Info, info =>
        {
            info.Property(i => i.Name).HasMaxLength(200).IsRequired();
            info.Property(i => i.Type).HasMaxLength(50).IsRequired();
            info.Property(i => i.Description).HasMaxLength(500);
        });
        
        builder.Property(d => d.TokenStatus)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(d => d.AccessToken).HasMaxLength(2000);
        
        builder.Property(d => d.TokenAcquiredAt);
        
        builder.Property(d => d.TokenExpiresAt);
        
        builder.Property(d => d.CreatedAt).IsRequired();
        
        builder.Property(d => d.UpdatedAt);
        
        builder.Ignore(d => d.DomainEvents);
    }
}
