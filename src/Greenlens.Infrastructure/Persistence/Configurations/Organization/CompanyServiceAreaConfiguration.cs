using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations.Organization;

internal sealed class CompanyServiceAreaConfiguration : IEntityTypeConfiguration<CompanyServiceArea>
{
    public void Configure(EntityTypeBuilder<CompanyServiceArea> builder)
    {
        builder.ToTable("company_service_areas");

        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.WardCode).IsRequired().HasMaxLength(20);

        // Unique: 1 company can only have 1 entry per ward
        builder.HasIndex(sa => new { sa.CompanyId, sa.WardCode }).IsUnique();
        builder.HasIndex(sa => sa.WardCode);

        builder.HasOne(sa => sa.Company)
            .WithMany(c => c.ServiceAreas)
            .HasForeignKey(sa => sa.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sa => sa.Ward)
            .WithMany()
            .HasForeignKey(sa => sa.WardCode)
            .HasPrincipalKey(w => w.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
