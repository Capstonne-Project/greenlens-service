using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class StaffInvitationConfiguration : IEntityTypeConfiguration<StaffInvitation>
{
    public void Configure(EntityTypeBuilder<StaffInvitation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(e => e.Token).IsUnique();

        builder.HasIndex(e => new { e.InvitedUserId, e.Status });

        builder.HasIndex(e => new { e.LocalOfficeId, e.Status });

        builder.HasOne(e => e.InvitedByUser)
            .WithMany()
            .HasForeignKey(e => e.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.InvitedUser)
            .WithMany()
            .HasForeignKey(e => e.InvitedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LocalOffice)
            .WithMany()
            .HasForeignKey(e => e.LocalOfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Team)
            .WithMany()
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.TargetRole)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
