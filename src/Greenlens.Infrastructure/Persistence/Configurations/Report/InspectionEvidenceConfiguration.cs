using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class InspectionEvidenceConfiguration : IEntityTypeConfiguration<InspectionEvidence>
{
    public void Configure(EntityTypeBuilder<InspectionEvidence> builder)
    {
        builder.ToTable("inspection_evidences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.MediaUrl).HasMaxLength(2000);
        builder.Property(e => e.MimeType).HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(2000);

        builder.HasOne(e => e.InspectionReport)
            .WithMany(ir => ir.Evidences)
            .HasForeignKey(e => e.InspectionReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.InspectionReportId);
        builder.HasIndex(e => new { e.InspectionReportId, e.Category });
    }
}
