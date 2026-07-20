using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class ReportWasteTagConfiguration : IEntityTypeConfiguration<ReportWasteTag>
{
    public void Configure(EntityTypeBuilder<ReportWasteTag> builder)
    {
        builder.ToTable("report_waste_tags");

        builder.HasKey(rt => new { rt.ReportId, rt.WasteTagId });

        builder.HasIndex(rt => rt.ReportId);
        builder.HasIndex(rt => rt.WasteTagId);

        builder.HasOne(rt => rt.Report)
            .WithMany(r => r.WasteTags)
            .HasForeignKey(rt => rt.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rt => rt.WasteTag)
            .WithMany(t => t.ReportWasteTags)
            .HasForeignKey(rt => rt.WasteTagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rt => rt.TaggedByUser)
            .WithMany()
            .HasForeignKey(rt => rt.TaggedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
