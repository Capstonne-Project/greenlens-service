using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class AssignmentProgressUpdateConfiguration : IEntityTypeConfiguration<AssignmentProgressUpdate>
{
    public void Configure(EntityTypeBuilder<AssignmentProgressUpdate> builder)
    {
        builder.ToTable("assignment_progress_updates");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.ProgressNote).HasMaxLength(1000);

        builder.HasOne(u => u.Assignment)
            .WithMany(a => a.ProgressUpdates)
            .HasForeignKey(u => u.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.UpdatedByUser)
            .WithMany()
            .HasForeignKey(u => u.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.AssignmentId);
        builder.HasIndex(u => u.ReportId);
        builder.HasIndex(u => new { u.AssignmentId, u.CreatedAt });
    }
}
