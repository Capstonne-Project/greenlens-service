using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for listing user's notifications sorted by date
        builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("ix_notifications_recipient_read_created");

        // Index for anti-spam counting by type per day (BR-NTF-003)
        builder.HasIndex(n => new { n.RecipientId, n.Type, n.CreatedAt })
            .HasDatabaseName("ix_notifications_recipient_type_created");
    }
}
