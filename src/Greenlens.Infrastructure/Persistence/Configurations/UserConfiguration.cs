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

        builder.Property(u => u.AvatarUrl).HasMaxLength(1000);
        builder.Property(u => u.GoogleId).HasMaxLength(128);
        builder.HasIndex(u => u.GoogleId).IsUnique().HasFilter("google_id IS NOT NULL");

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(50);

        // Soft delete query filter
        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
