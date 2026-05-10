namespace Greenlens.Infrastructure.Persistence.Configurations.Location;

using Greenlens.Domain.Entities.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("provinces");

        builder.HasKey(p => p.Code);
        builder.Property(p => p.Code).HasMaxLength(2).IsRequired().IsFixedLength();
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.BoundaryUrl).HasMaxLength(500);

        builder.HasOne(p => p.AdministrativeRegion)
            .WithMany(r => r.Provinces)
            .HasForeignKey(p => p.AdministrativeRegionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.AdministrativeUnit)
            .WithMany(u => u.Provinces)
            .HasForeignKey(p => p.AdministrativeUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.AdministrativeRegionId);
    }
}
