using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202606302050_SeedBadgeData12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000001"),
                columns: new[] { "description", "icon_url", "name_en", "name_vi" },
                values: new object[] { "Gửi báo cáo ô nhiễm đầu tiên được xác minh", "badges/icons/first_report.png", "First Reporter", "Người Khởi Đầu" });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000002"),
                columns: new[] { "icon_url", "name_vi" },
                values: new object[] { "badges/icons/eco_warrior.png", "Chiến Binh Xanh" });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000003"),
                columns: new[] { "code", "description", "icon_url", "name_en", "name_vi", "required_report_count" },
                values: new object[] { "green_champion", "Gửi 50 báo cáo ô nhiễm được xác minh", "badges/icons/green_champion.png", "Green Champion", "Nhà Vô Địch Xanh", 50 });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000004"),
                columns: new[] { "code", "description", "icon_url", "name_en", "name_vi", "required_report_count" },
                values: new object[] { "earth_guardian", "Gửi 100 báo cáo ô nhiễm được xác minh", "badges/icons/earth_guardian.png", "Earth Guardian", "Người Bảo Vệ Trái Đất", 100 });

            migrationBuilder.InsertData(
                table: "badges",
                columns: new[] { "id", "code", "created_at", "description", "icon_url", "is_active", "name_en", "name_vi", "required_points", "required_report_count" },
                values: new object[,]
                {
                    { new Guid("a1000001-0000-0000-0000-000000000005"), "streak_7d", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gửi báo cáo 7 ngày liên tiếp", "badges/icons/streak_7d.png", true, "7-Day Streak", "Bền Bỉ 7 Ngày", null, null },
                    { new Guid("a1000001-0000-0000-0000-000000000006"), "streak_30d", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gửi báo cáo 30 ngày liên tiếp", "badges/icons/streak_30d.png", true, "30-Day Streak", "Kiên Trì 30 Ngày", null, null },
                    { new Guid("a1000001-0000-0000-0000-000000000007"), "hotspot_hunter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gửi 3 báo cáo trong khu vực hotspot ô nhiễm", "badges/icons/hotspot_hunter.png", true, "Hotspot Hunter", "Thợ Săn Điểm Nóng", null, null },
                    { new Guid("a1000001-0000-0000-0000-000000000008"), "duplicate_finder", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "5 báo cáo được xác nhận là trùng lặp, hỗ trợ phát hiện ô nhiễm", "badges/icons/duplicate_finder.png", true, "Duplicate Finder", "Người Phát Hiện Trùng", null, null },
                    { new Guid("a1000001-0000-0000-0000-000000000009"), "community_voice", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Có báo cáo nhận ≥ 10 lượt xác nhận từ cộng đồng", "badges/icons/community_voice.png", true, "Community Voice", "Tiếng Nói Cộng Đồng", null, null },
                    { new Guid("a1000001-0000-0000-0000-000000000010"), "rising_star", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Đạt Level 2 với 100 điểm tích lũy", "badges/icons/rising_star.png", true, "Rising Star", "Ngôi Sao Đang Lên", 100, null },
                    { new Guid("a1000001-0000-0000-0000-000000000011"), "eco_expert", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Đạt Level 4 với 1.500 điểm tích lũy", "badges/icons/eco_expert.png", true, "Eco Expert", "Chuyên Gia Môi Trường", 1500, null },
                    { new Guid("a1000001-0000-0000-0000-000000000012"), "green_legend", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Đạt Level 5 với 5.000 điểm tích lũy — thành tựu cao nhất", "badges/icons/green_legend.png", true, "Green Legend", "Huyền Thoại Xanh", 5000, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000012"));

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000001"),
                columns: new[] { "description", "icon_url", "name_en", "name_vi" },
                values: new object[] { "Gửi báo cáo ô nhiễm đầu tiên", null, "First Report", "Người khởi đầu" });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000002"),
                columns: new[] { "icon_url", "name_vi" },
                values: new object[] { null, "Chiến binh Xanh" });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000003"),
                columns: new[] { "code", "description", "icon_url", "name_en", "name_vi", "required_report_count" },
                values: new object[] { "hotspot_hunter", "Gửi 3 báo cáo trong vùng hotspot", null, "Hotspot Hunter", "Thợ săn điểm nóng", null });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000004"),
                columns: new[] { "code", "description", "icon_url", "name_en", "name_vi", "required_report_count" },
                values: new object[] { "streak_7d", "Gửi báo cáo 7 ngày liên tiếp", null, "7-Day Streak", "7 ngày liên tiếp", null });
        }
    }
}
