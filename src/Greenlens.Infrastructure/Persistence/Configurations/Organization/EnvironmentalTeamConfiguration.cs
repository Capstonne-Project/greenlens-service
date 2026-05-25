using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations.Organization;

internal sealed class EnvironmentalTeamConfiguration : IEntityTypeConfiguration<EnvironmentalTeam>
{
    public void Configure(EntityTypeBuilder<EnvironmentalTeam> builder)
    {
        builder.ToTable("environmental_teams");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.TeamType).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(t => t.LocalOfficeId);
        builder.HasIndex(t => t.TeamType);

        builder.HasOne(t => t.LocalOffice)
            .WithMany(lo => lo.Teams)
            .HasForeignKey(t => t.LocalOfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Members)
            .WithOne(m => m.Team)
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
