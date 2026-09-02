using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202609031200_FixGamificationPointsAndConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_point_tx_idempotent",
                table: "point_transactions");

            // Idempotent — prod có thể đã có row từ GamificationSeeder runtime.
            migrationBuilder.Sql("""
                INSERT INTO gamification_configs (id, action_type, points, is_active, description, created_at)
                SELECT 'a0000001-0000-0000-0000-000000000007', 'CommunityCleanupParticipation', 15, true,
                       'Tham gia và check-in một chương trình dọn dẹp cộng đồng đã hoàn thành',
                       TIMESTAMPTZ '2026-07-10 00:00:00+00'
                WHERE NOT EXISTS (
                    SELECT 1 FROM gamification_configs WHERE action_type = 'CommunityCleanupParticipation'
                );
                """);

            migrationBuilder.Sql("""
                UPDATE badges SET required_action_count = 2
                WHERE code = 'cleanup_hero' AND (required_action_count IS NULL OR required_action_count <> 2);
                """);

            migrationBuilder.CreateIndex(
                name: "ix_point_tx_idempotent",
                table: "point_transactions",
                columns: new[] { "user_points_id", "report_id", "reason" },
                unique: true,
                filter: "\"report_id\" IS NOT NULL AND \"deleted_at\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_point_tx_idempotent",
                table: "point_transactions");

            migrationBuilder.Sql("""
                DELETE FROM gamification_configs
                WHERE id = 'a0000001-0000-0000-0000-000000000007'
                  AND action_type = 'CommunityCleanupParticipation';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_point_tx_idempotent",
                table: "point_transactions",
                columns: new[] { "user_points_id", "report_id", "reason" },
                unique: true,
                filter: "\"report_id\" IS NOT NULL");
        }
    }
}
