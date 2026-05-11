using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class ReportMediaConfiguration : IEntityTypeConfiguration<ReportMedia>
{
    public void Configure(EntityTypeBuilder<ReportMedia> builder)
    {
        builder.ToTable("report_media");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Url).IsRequired().HasMaxLength(500);
        builder.Property(m => m.ThumbnailUrl).HasMaxLength(500);
        builder.Property(m => m.MimeType).IsRequired().HasMaxLength(50);
        builder.Property(m => m.PHash).HasMaxLength(64);
        builder.Property(m => m.ExifData).HasColumnType("jsonb");

        // ── Relationships ──
        builder.HasOne(m => m.Report)
            .WithMany(r => r.Media)
            .HasForeignKey(m => m.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Uploader)
            .WithMany()
            .HasForeignKey(m => m.UploadedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Indexes ──
        builder.HasIndex(m => m.ReportId);
        builder.HasIndex(m => m.PHash);
    }
}
