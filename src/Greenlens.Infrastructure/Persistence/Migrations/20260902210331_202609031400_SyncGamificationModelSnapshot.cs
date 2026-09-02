using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202609031400_SyncGamificationModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "gamification_configs",
                keyColumn: "id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000006"),
                column: "description",
                value: "Phạt gian lận — trừ toàn bộ điểm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "gamification_configs",
                keyColumn: "id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000006"),
                column: "description",
                value: "BR-GAM-006: Phạt gian lận — trừ toàn bộ điểm");
        }
    }
}
