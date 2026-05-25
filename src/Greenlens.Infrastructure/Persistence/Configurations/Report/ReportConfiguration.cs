using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).IsRequired().HasMaxLength(20);
        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.Address).HasMaxLength(500);

        builder.Property(r => r.Latitude).HasPrecision(10, 7);
        builder.Property(r => r.Longitude).HasPrecision(10, 7);

        builder.Property(r => r.WardCode).HasMaxLength(5);
        builder.Property(r => r.ProvinceCode).HasMaxLength(2);

        builder.Property(r => r.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.SeveritySetBy).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.AiEstimatedSeverity).HasConversion<string>().HasMaxLength(20);

        builder.Property(r => r.AiClassifiedType).HasMaxLength(50);
        builder.Property(r => r.AiConfidence).HasPrecision(3, 2);
        builder.Property(r => r.PriorityScore).HasPrecision(8, 2);

        // SuspiciousReasons stored as JSONB
        builder.Property(r => r.SuspiciousReasons).HasColumnType("jsonb");

        builder.Property(r => r.RejectedReason).HasMaxLength(2000);

        // ── Relationships ──
        builder.HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Category)
            .WithMany(c => c.Reports)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ParentReport)
            .WithMany(r => r.DuplicateReports)
            .HasForeignKey(r => r.ParentReportId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.VerifiedByUser)
            .WithMany()
            .HasForeignKey(r => r.VerifiedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Organization assignment ──
        builder.HasOne(r => r.AssignedOffice)
            .WithMany()
            .HasForeignKey(r => r.AssignedOfficeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.AssignedDepartment)
            .WithMany()
            .HasForeignKey(r => r.AssignedDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Indexes ──
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CategoryId);
        builder.HasIndex(r => r.Severity);
        builder.HasIndex(r => r.AssignedOfficerId);
        builder.HasIndex(r => r.AssignedByOfficerId);
        builder.HasIndex(r => r.AssignedOfficeId);
        builder.HasIndex(r => r.AssignedDepartmentId);
        builder.HasIndex(r => r.WardCode);
        builder.HasIndex(r => r.ProvinceCode);
        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => r.ParentReportId);

        // Soft delete query filter
        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
