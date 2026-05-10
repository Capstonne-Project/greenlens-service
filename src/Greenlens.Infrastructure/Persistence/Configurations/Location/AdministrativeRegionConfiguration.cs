namespace Greenlens.Infrastructure.Persistence.Configurations.Location;

using Greenlens.Domain.Entities.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class AdministrativeRegionConfiguration : IEntityTypeConfiguration<AdministrativeRegion>
{
    public void Configure(EntityTypeBuilder<AdministrativeRegion> builder)
    {
        // EFCore.NamingConventions sẽ tự đổi ra snake_case ở DB.
        builder.ToTable("administrative_regions");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();        // ID 1-8 cố định, do seeder set
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
    }
}
