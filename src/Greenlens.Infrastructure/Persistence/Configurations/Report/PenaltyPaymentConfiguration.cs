using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class PenaltyPaymentConfiguration : IEntityTypeConfiguration<PenaltyPayment>
{
    public void Configure(EntityTypeBuilder<PenaltyPayment> builder)
    {
        builder.ToTable("penalty_payments");

        builder.HasKey(pp => pp.Id);

        // ── Decimal precision ──
        builder.Property(pp => pp.Amount).HasPrecision(18, 2);

        // ── String lengths ──
        builder.Property(pp => pp.EvidenceUrl).HasMaxLength(500);
        builder.Property(pp => pp.Note).HasMaxLength(1000);

        // ── Relationships ──
        builder.HasOne(pp => pp.InspectionReport)
            .WithMany(ir => ir.Payments)
            .HasForeignKey(pp => pp.InspectionReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.RecordedByUser)
            .WithMany()
            .HasForeignKey(pp => pp.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes ──
        builder.HasIndex(pp => pp.InspectionReportId);
        builder.HasIndex(pp => pp.RecordedByUserId);

        builder.HasQueryFilter(pp => pp.DeletedAt == null);
    }
}
