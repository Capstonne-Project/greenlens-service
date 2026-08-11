using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class DeleteHotspotHunterBadge : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // hotspot_hunter has no eligibility/progress logic wired up (no Hotspot concept
        // exists in the domain) and was previously deactivated. Rather than keep a dead,
        // never-earnable row around, remove it entirely.
        //
        // Defensive delete of user_badges first: IsEligible never had a case for
        // "hotspot_hunter", so no row should exist, but this guards against FK violation
        // if one somehow does (badges.id -> user_badges.badge_id is ON DELETE CASCADE
        // anyway, so this is belt-and-suspenders).
        migrationBuilder.Sql("""
            DELETE FROM user_badges
            WHERE badge_id = (SELECT id FROM badges WHERE code = 'hotspot_hunter');

            DELETE FROM badges
            WHERE code = 'hotspot_hunter';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Irreversible: the deleted badge's original id/timestamps cannot be reliably
        // restored, and any user_badges rows removed above are gone for good. No-op.
    }
}
