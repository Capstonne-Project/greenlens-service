using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class PenaltyFrameworkConfiguration : IEntityTypeConfiguration<PenaltyFramework>
{
    public void Configure(EntityTypeBuilder<PenaltyFramework> builder)
    {
        builder.ToTable("penalty_frameworks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ViolationLevel)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.MinAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.MaxAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("VND");

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite index: lookup by category + level + active status
        builder.HasIndex(x => new { x.CategoryId, x.ViolationLevel, x.IsActive })
            .HasDatabaseName("ix_penalty_fw_category_level_active");
    }
}
