using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class ReportFlagConfiguration : IEntityTypeConfiguration<ReportFlag>
{
    public void Configure(EntityTypeBuilder<ReportFlag> builder)
    {
        builder.ToTable("report_flags");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FlagType).HasConversion<string>().HasMaxLength(30);
        builder.Property(f => f.Reason).HasMaxLength(2000);

        // Unique constraint: one flag per type per user per report
        builder.HasIndex(f => new { f.ReportId, f.FlaggerId, f.FlagType }).IsUnique();

        // ── Relationships ──
        builder.HasOne(f => f.Report)
            .WithMany(r => r.Flags)
            .HasForeignKey(f => f.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Flagger)
            .WithMany()
            .HasForeignKey(f => f.FlaggerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
