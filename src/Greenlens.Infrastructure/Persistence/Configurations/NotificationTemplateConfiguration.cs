using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateKey).HasMaxLength(100);
        builder.Property(x => x.TitleVi).HasMaxLength(500);
        builder.Property(x => x.BodyVi).HasMaxLength(4000);
        builder.Property(x => x.TitleEn).HasMaxLength(500);
        builder.Property(x => x.BodyEn).HasMaxLength(4000);

        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);

        builder.Property(x => x.IsPublished).HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasIndex(x => new { x.TemplateKey, x.Channel })
            .IsUnique()
            .HasDatabaseName("ix_notification_template_key_channel");
    }
}
