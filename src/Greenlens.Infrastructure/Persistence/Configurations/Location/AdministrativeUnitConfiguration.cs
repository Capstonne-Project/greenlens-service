namespace Greenlens.Infrastructure.Persistence.Configurations.Location;

using Greenlens.Domain.Entities.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class AdministrativeUnitConfiguration : IEntityTypeConfiguration<AdministrativeUnit>
{
    public void Configure(EntityTypeBuilder<AdministrativeUnit> builder)
    {
        builder.ToTable("administrative_units");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();        // ID 1-5 cố định
        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Abbreviation).HasMaxLength(50).IsRequired();
    }
}
