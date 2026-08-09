using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _202608092000_AddCleanupHeroBadge : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Idempotent: dev DBs may already have cleanup_hero from GamificationSeeder (random id).
        migrationBuilder.Sql("""
            INSERT INTO badges (
                id, code, name_vi, name_en, description, is_active,
                required_report_count, required_points, required_streak_days,
                icon_url, created_at)
            SELECT
                'a1000001-0000-0000-0000-000000000013'::uuid,
                'cleanup_hero',
                'Anh Hùng Dọn Dẹp',
                'Cleanup Hero',
                'Hoàn thành tham gia 2 chương trình dọn dẹp cộng đồng',
                TRUE,
                NULL,
                NULL,
                NULL,
                'badges/icons/cleanup_hero.png',
                TIMESTAMPTZ '2026-01-01 00:00:00+00'
            WHERE NOT EXISTS (
                SELECT 1 FROM badges WHERE code = 'cleanup_hero'
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM badges
            WHERE id = 'a1000001-0000-0000-0000-000000000013'::uuid
              AND code = 'cleanup_hero';
            """);
    }
}
