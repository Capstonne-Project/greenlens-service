using Greenlens.Domain.Entities;
using Greenlens.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class ReportStatusHistoryConfiguration : IEntityTypeConfiguration<ReportStatusHistory>
{
    public void Configure(EntityTypeBuilder<ReportStatusHistory> builder)
    {
        builder.ToTable("report_status_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.FromStatus)
            .HasConversion(new LegacyNullableReportStatusValueConverter())
            .HasMaxLength(20);
        builder.Property(h => h.ToStatus)
            .HasConversion(new LegacyReportStatusValueConverter())
            .HasMaxLength(20);
        builder.Property(h => h.Reason).HasMaxLength(2000);
        builder.Property(h => h.Metadata).HasColumnType("jsonb");

        // ── Relationships ──
        builder.HasOne(h => h.Report)
            .WithMany(r => r.StatusHistory)
            .HasForeignKey(h => h.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Indexes ──
        builder.HasIndex(h => h.ReportId);
        builder.HasIndex(h => h.CreatedAt);
    }
}
