using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations.Organization;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.ProvinceCode).IsRequired().HasMaxLength(2);

        // 1 Department per Province (BR-ORG-001)
        builder.HasIndex(d => d.ProvinceCode).IsUnique();

        builder.HasOne(d => d.Province)
            .WithMany()
            .HasForeignKey(d => d.ProvinceCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.LocalOffices)
            .WithOne(lo => lo.Department)
            .HasForeignKey(lo => lo.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
