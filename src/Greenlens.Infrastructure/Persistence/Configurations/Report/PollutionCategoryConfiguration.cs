using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class PollutionCategoryConfiguration : IEntityTypeConfiguration<PollutionCategory>
{
    public void Configure(EntityTypeBuilder<PollutionCategory> builder)
    {
        builder.ToTable("pollution_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.NameVi).IsRequired().HasMaxLength(100);
        builder.Property(c => c.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(c => c.IconUrl).HasMaxLength(500);
    }
}
