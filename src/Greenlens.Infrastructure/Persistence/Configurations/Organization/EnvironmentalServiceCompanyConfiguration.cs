using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations.Organization;

internal sealed class EnvironmentalServiceCompanyConfiguration
    : IEntityTypeConfiguration<EnvironmentalServiceCompany>
{
    public void Configure(EntityTypeBuilder<EnvironmentalServiceCompany> builder)
    {
        builder.ToTable("environmental_service_companies");

        builder.HasKey(c => c.Id);

        // ── Core info ──
        builder.Property(c => c.Name).IsRequired().HasMaxLength(300);
        builder.Property(c => c.TaxCode).HasMaxLength(20);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(200);

        // ── Contract ──
        builder.Property(c => c.ContractNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.ContractNumber).IsUnique();

        // ── Status & ContractType enums as string ──
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.ContractType).HasConversion<string>().HasMaxLength(20);


        // ── Relationships ──
        builder.HasOne(c => c.Department)
            .WithMany()
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Staff)
            .WithOne(s => s.Company)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes ──
        builder.HasIndex(c => c.DepartmentId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.TaxCode);

        // ── Soft delete filter ──
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
