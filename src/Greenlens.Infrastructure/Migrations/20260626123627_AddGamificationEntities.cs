using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGamificationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "badges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_vi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    icon_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    required_points = table.Column<int>(type: "integer", nullable: true),
                    required_report_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_badges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    locked_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_points", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_points_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_badges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    badge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    awarded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_badges", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_badges_badges_badge_id",
                        column: x => x.badge_id,
                        principalTable: "badges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_badges_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "point_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_points_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_point_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_point_transactions_user_points_user_points_id",
                        column: x => x.user_points_id,
                        principalTable: "user_points",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "badges",
                columns: new[] { "id", "code", "created_at", "description", "icon_url", "is_active", "name_en", "name_vi", "required_points", "required_report_count" },
                values: new object[,]
                {
                    { new Guid("a1000001-0000-0000-0000-000000000001"), "first_report", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gửi báo cáo ô nhiễm đầu tiên", null, true, "First Report", "Người khởi đầu", null, 1 },
                    { new Guid("a1000001-0000-0000-0000-000000000002"), "eco_warrior", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gửi 10 báo cáo ô nhiễm được xác minh", null, true, "Eco Warrior", "Chiến binh Xanh", null, 10 },
                    { new Guid("a1000001-0000-0000-0000-000000000003"), "hotspot_hunter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gửi 3 báo cáo trong vùng hotspot", null, true, "Hotspot Hunter", "Thợ săn điểm nóng", null, null },
                    { new Guid("a1000001-0000-0000-0000-000000000004"), "streak_7d", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gửi báo cáo 7 ngày liên tiếp", null, true, "7-Day Streak", "7 ngày liên tiếp", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_badges_code",
                table: "badges",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_point_tx_created",
                table: "point_transactions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_point_tx_idempotent",
                table: "point_transactions",
                columns: new[] { "user_points_id", "report_id", "reason" },
                unique: true,
                filter: "\"report_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_badge_unique",
                table: "user_badges",
                columns: new[] { "user_id", "badge_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_badges_badge_id",
                table: "user_badges",
                column: "badge_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_points_leaderboard",
                table: "user_points",
                columns: new[] { "is_locked", "total_points" });

            migrationBuilder.CreateIndex(
                name: "ix_user_points_user_id",
                table: "user_points",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "point_transactions");

            migrationBuilder.DropTable(
                name: "user_badges");

            migrationBuilder.DropTable(
                name: "user_points");

            migrationBuilder.DropTable(
                name: "badges");
        }
    }
}
