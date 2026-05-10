using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class ReportDraftConfiguration : IEntityTypeConfiguration<ReportDraft>
{
    public void Configure(EntityTypeBuilder<ReportDraft> builder)
    {
        builder.ToTable("report_drafts");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Payload).IsRequired().HasColumnType("jsonb");

        // ── Relationships ──
        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes ──
        builder.HasIndex(d => d.UserId);
    }
}
