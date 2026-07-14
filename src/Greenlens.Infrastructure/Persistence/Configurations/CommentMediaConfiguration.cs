using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class CommentMediaConfiguration : IEntityTypeConfiguration<CommentMedia>
{
    public void Configure(EntityTypeBuilder<CommentMedia> builder)
    {
        builder.ToTable("comment_media");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Url).IsRequired().HasMaxLength(500);
        builder.Property(m => m.MimeType).IsRequired().HasMaxLength(50);

        builder.HasOne(m => m.Comment)
            .WithMany(c => c.Media)
            .HasForeignKey(m => m.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.CommentId);
    }
}
