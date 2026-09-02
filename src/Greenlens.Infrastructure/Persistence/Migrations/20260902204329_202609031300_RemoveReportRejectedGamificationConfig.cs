using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202609031300_RemoveReportRejectedGamificationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "gamification_configs",
                keyColumn: "id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000005"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "gamification_configs",
                columns: new[] { "id", "action_type", "created_at", "created_by", "description", "is_active", "points", "updated_at", "updated_by" },
                values: new object[] { new Guid("a0000001-0000-0000-0000-000000000005"), "ReportRejected", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "Báo cáo bị từ chối (không hợp lệ)", true, -5, null, null });
        }
    }
}
