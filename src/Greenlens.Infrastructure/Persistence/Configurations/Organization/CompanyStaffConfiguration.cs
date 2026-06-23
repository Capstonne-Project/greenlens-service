using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations.Organization;

internal sealed class CompanyStaffConfiguration : IEntityTypeConfiguration<CompanyStaff>
{
    public void Configure(EntityTypeBuilder<CompanyStaff> builder)
    {
        builder.ToTable("company_staff");

        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.Position).HasMaxLength(100);

        // ── Unique constraint: one user per company ──
        builder.HasIndex(cs => new { cs.UserId, cs.CompanyId }).IsUnique();

        // ── Relationships ──
        builder.HasOne(cs => cs.User)
            .WithMany()
            .HasForeignKey(cs => cs.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cs => cs.Company)
            .WithMany(c => c.Staff)
            .HasForeignKey(cs => cs.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes ──
        builder.HasIndex(cs => cs.CompanyId);
        builder.HasIndex(cs => cs.IsActive);
    }
}
