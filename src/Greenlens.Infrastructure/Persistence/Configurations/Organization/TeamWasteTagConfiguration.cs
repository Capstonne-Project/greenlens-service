using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations.Organization;

internal sealed class TeamWasteTagConfiguration : IEntityTypeConfiguration<TeamWasteTag>
{
    public void Configure(EntityTypeBuilder<TeamWasteTag> builder)
    {
        builder.ToTable("team_waste_tags");

        builder.HasKey(tw => new { tw.TeamId, tw.WasteTagId });

        builder.HasIndex(tw => tw.WasteTagId);

        builder.HasOne(tw => tw.Team)
            .WithMany(t => t.WasteTags)
            .HasForeignKey(tw => tw.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tw => tw.WasteTag)
            .WithMany(t => t.TeamWasteTags)
            .HasForeignKey(tw => tw.WasteTagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
