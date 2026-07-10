using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations.Organization;

internal sealed class ContractPeriodConfiguration
    : IEntityTypeConfiguration<ContractPeriod>
{
    public void Configure(EntityTypeBuilder<ContractPeriod> builder)
    {
        builder.ToTable("contract_periods");

        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.ContractNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cp => cp.ContractType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(cp => cp.Note)
            .HasMaxLength(500);

        // ── Relationships ──
        builder.HasOne(cp => cp.Company)
            .WithMany(c => c.ContractPeriods)
            .HasForeignKey(cp => cp.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes ──
        builder.HasIndex(cp => cp.CompanyId);
        builder.HasIndex(cp => new { cp.CompanyId, cp.StartDate });
    }
}
