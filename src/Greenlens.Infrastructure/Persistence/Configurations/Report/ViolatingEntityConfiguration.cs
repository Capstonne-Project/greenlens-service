using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class ViolatingEntityConfiguration : IEntityTypeConfiguration<ViolatingEntity>
{
    public void Configure(EntityTypeBuilder<ViolatingEntity> builder)
    {
        builder.ToTable("violating_entities");

        builder.HasKey(ve => ve.Id);

        // ── Type as string ──
        builder.Property(ve => ve.Type).HasConversion<string>().HasMaxLength(20);

        // ── String lengths ──
        builder.Property(ve => ve.Name).HasMaxLength(200).IsRequired();
        builder.Property(ve => ve.Address).HasMaxLength(500);
        builder.Property(ve => ve.TaxCode).HasMaxLength(20);
        builder.Property(ve => ve.IdentityNumber).HasMaxLength(20);
        builder.Property(ve => ve.PhoneNumber).HasMaxLength(20);

        // ── Indexes ──
        // TaxCode unique (filtered — only Business entities have TaxCode)
        builder.HasIndex(ve => ve.TaxCode)
            .IsUnique()
            .HasFilter("tax_code IS NOT NULL");

        // IdentityNumber unique (filtered — only Individual entities have IdentityNumber)
        builder.HasIndex(ve => ve.IdentityNumber)
            .IsUnique()
            .HasFilter("identity_number IS NOT NULL");

        builder.HasIndex(ve => ve.Name);

        // ── Relationships ──
        builder.HasMany(ve => ve.InspectionReports)
            .WithOne(ir => ir.ViolatingEntity)
            .HasForeignKey(ir => ir.ViolatingEntityId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Soft delete filter ──
        builder.HasQueryFilter(ve => ve.DeletedAt == null);
    }
}
