namespace Greenlens.Infrastructure.Persistence.Configurations.Location;

using Greenlens.Domain.Entities.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("wards");

        builder.HasKey(w => w.Code);
        builder.Property(w => w.Code).HasMaxLength(5).IsRequired().IsFixedLength();
        builder.Property(w => w.Name).HasMaxLength(150).IsRequired();
        builder.Property(w => w.ProvinceCode).HasMaxLength(2).IsRequired().IsFixedLength();
        builder.Property(w => w.BoundaryUrl).HasMaxLength(500);

        builder.HasOne(w => w.Province)
            .WithMany(p => p.Wards)
            .HasForeignKey(w => w.ProvinceCode)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.AdministrativeUnit)
            .WithMany(u => u.Wards)
            .HasForeignKey(w => w.AdministrativeUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => w.ProvinceCode);
        builder.HasIndex(w => w.Name);
    }
}
