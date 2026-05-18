using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class ReportAssignmentConfiguration : IEntityTypeConfiguration<ReportAssignment>
{
    public void Configure(EntityTypeBuilder<ReportAssignment> builder)
    {
        builder.ToTable("report_assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Note).HasMaxLength(500);
        builder.Property(a => a.DeclineReason).HasMaxLength(500);
        builder.Property(a => a.ProgressNote).HasMaxLength(1000);
        builder.Property(a => a.ProgressPercent).HasDefaultValue(0);

        // A team may appear multiple times (e.g. declined then re-assigned)
        builder.HasIndex(a => new { a.ReportId, a.TeamId });
        builder.HasIndex(a => a.ReportId);
        builder.HasIndex(a => a.TeamId);
        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Report)
            .WithMany(r => r.Assignments)
            .HasForeignKey(a => a.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Team)
            .WithMany()
            .HasForeignKey(a => a.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedByUser)
            .WithMany()
            .HasForeignKey(a => a.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
