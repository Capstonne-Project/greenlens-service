using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class InspectionReportConfiguration : IEntityTypeConfiguration<InspectionReport>
{
    public void Configure(EntityTypeBuilder<InspectionReport> builder)
    {
        builder.ToTable("inspection_reports");

        builder.HasKey(ir => ir.Id);

        // ── Enums as string ──
        builder.Property(ir => ir.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(ir => ir.ViolationLevel).HasConversion<string>().HasMaxLength(20);

        // ── Violation details ──
        builder.Property(ir => ir.ViolationDescription).HasMaxLength(2000);
        builder.Property(ir => ir.ViolatorName).HasMaxLength(200);
        builder.Property(ir => ir.ViolatorAddress).HasMaxLength(500);
        builder.Property(ir => ir.ViolatorIdentity).HasMaxLength(50);

        // ── Penalty ──
        builder.Property(ir => ir.PenaltyAmount).HasPrecision(18, 2);
        builder.Property(ir => ir.PenaltyDecisionNumber).HasMaxLength(50);
        builder.Property(ir => ir.PaidAmount).HasPrecision(18, 2);
        builder.Property(ir => ir.AdditionalPenaltyMeasures).HasMaxLength(1000);

        // ── Close ──
        builder.Property(ir => ir.ClosedReason).HasMaxLength(2000);

        // ── Relationships ──
        builder.HasOne(ir => ir.Report)
            .WithMany()
            .HasForeignKey(ir => ir.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ir => ir.CreatedByOfficer)
            .WithMany()
            .HasForeignKey(ir => ir.CreatedByOfficerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ir => ir.IssuedByInspector)
            .WithMany()
            .HasForeignKey(ir => ir.IssuedByInspectorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ir => ir.AssignedTeam)
            .WithMany()
            .HasForeignKey(ir => ir.AssignedTeamId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Indexes ──
        builder.HasIndex(ir => ir.ReportId);
        builder.HasIndex(ir => ir.Status);
        builder.HasIndex(ir => ir.CreatedByOfficerId);
        builder.HasIndex(ir => ir.AssignedTeamId);
        builder.HasIndex(ir => ir.ViolatorIdentity);
    }
}
