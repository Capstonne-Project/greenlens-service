using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.HasIndex(u => u.PhoneNumber).IsUnique().HasFilter("phone_number IS NOT NULL");
        builder.Property(u => u.IsPhoneVerified).HasDefaultValue(false);

        builder.Property(u => u.AvatarUrl).HasMaxLength(1000);
        builder.Property(u => u.GoogleId).HasMaxLength(128);
        builder.HasIndex(u => u.GoogleId).IsUnique().HasFilter("google_id IS NOT NULL");

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(50);

        // ── Organization assignment (v1.1) ──
        builder.HasOne(u => u.Department)
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.LocalOffice)
            .WithMany()
            .HasForeignKey(u => u.LocalOfficeId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Data Consent (BR-DAT-005) ──
        builder.Property(u => u.HasDataConsent).HasDefaultValue(false);

        // ── Comment moderation (BR-CMT-003) ──
        builder.Property(u => u.CommentViolationCount).HasDefaultValue(0);

        // ── Gamification showcase (BR-GAM-004) — badge nổi bật trên hồ sơ ──
        builder.HasOne<Badge>()
            .WithMany()
            .HasForeignKey(u => u.FeaturedBadgeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Soft delete query filter
        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
