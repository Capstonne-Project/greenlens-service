using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations.Organization;

internal sealed class LocalOfficeConfiguration : IEntityTypeConfiguration<LocalOffice>
{
    public void Configure(EntityTypeBuilder<LocalOffice> builder)
    {
        builder.ToTable("local_offices");

        builder.HasKey(lo => lo.Id);

        builder.Property(lo => lo.Name).IsRequired().HasMaxLength(200);
        builder.Property(lo => lo.WardCode).IsRequired().HasMaxLength(5);

        // 1 Office per Ward (BR-ORG-002)
        builder.HasIndex(lo => lo.WardCode).IsUnique();
        builder.HasIndex(lo => lo.DepartmentId);

        builder.HasOne(lo => lo.Ward)
            .WithMany()
            .HasForeignKey(lo => lo.WardCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lo => lo.Department)
            .WithMany(d => d.LocalOffices)
            .HasForeignKey(lo => lo.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // LEO assigned to this office (nullable — office can exist without LEO initially)
        builder.HasOne(lo => lo.Officer)
            .WithMany()
            .HasForeignKey(lo => lo.OfficerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(lo => lo.Teams)
            .WithOne(t => t.LocalOffice)
            .HasForeignKey(t => t.LocalOfficeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
