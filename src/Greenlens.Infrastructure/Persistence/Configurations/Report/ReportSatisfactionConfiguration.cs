using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class ReportSatisfactionConfiguration : IEntityTypeConfiguration<ReportSatisfaction>
{
    public void Configure(EntityTypeBuilder<ReportSatisfaction> builder)
    {
        builder.ToTable("report_satisfactions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Comment).HasMaxLength(2000);

        // ── Relationships ──
        builder.HasOne(s => s.Report)
            .WithMany()
            .HasForeignKey(s => s.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes ──
        builder.HasIndex(s => s.ReportId);
    }
}
