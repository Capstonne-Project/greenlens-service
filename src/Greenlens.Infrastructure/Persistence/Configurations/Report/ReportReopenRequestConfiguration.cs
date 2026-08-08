using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class ReportReopenRequestConfiguration : IEntityTypeConfiguration<ReportReopenRequest>
{
    public void Configure(EntityTypeBuilder<ReportReopenRequest> builder)
    {
        builder.ToTable("report_reopen_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.RejectionReason).HasMaxLength(2000);

        builder.HasOne(r => r.Report)
            .WithMany(r => r.ReopenRequests)
            .HasForeignKey(r => r.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Requester)
            .WithMany()
            .HasForeignKey(r => r.RequestedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.ReportId, r.Status });
        builder.HasIndex(r => r.RequestedAt);

        // BR-REP-015: at most one Pending reopen request per report (race-safe).
        builder.HasIndex(r => r.ReportId)
            .IsUnique()
            .HasFilter("status = 'Pending'");
    }
}
