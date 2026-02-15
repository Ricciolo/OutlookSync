using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Infrastructure.Persistence.Configurations;

public class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("Credentials");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.TokenStatus)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(c => c.StatusData)
            .HasColumnType("BLOB")
            .HasColumnName("StatusData");
        
        builder.Property(c => c.CreatedAt).IsRequired();
        
        builder.Property(c => c.UpdatedAt);
    }
}
