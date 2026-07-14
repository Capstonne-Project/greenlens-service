using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202607132015_AddDuplicateDetectionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ai_similarity_score",
                table: "reports",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "duplicate_detection_source",
                table: "reports",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_possible_duplicate",
                table: "reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "possible_duplicate_of_report_id",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "gamification_configs",
                keyColumn: "id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000004"),
                column: "description",
                value: "Báo cáo trùng được gộp: +50% điểm báo cáo gốc (ReportVerified). Giá trị Points chỉ mang tính tham chiếu; runtime tính động.");

            migrationBuilder.CreateIndex(
                name: "ix_reports_is_possible_duplicate",
                table: "reports",
                column: "is_possible_duplicate");

            migrationBuilder.CreateIndex(
                name: "ix_reports_possible_duplicate_of_report_id",
                table: "reports",
                column: "possible_duplicate_of_report_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reports_reports_possible_duplicate_of_report_id",
                table: "reports",
                column: "possible_duplicate_of_report_id",
                principalTable: "reports",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reports_reports_possible_duplicate_of_report_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "ix_reports_is_possible_duplicate",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "ix_reports_possible_duplicate_of_report_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "ai_similarity_score",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "duplicate_detection_source",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "is_possible_duplicate",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "possible_duplicate_of_report_id",
                table: "reports");

            migrationBuilder.UpdateData(
                table: "gamification_configs",
                keyColumn: "id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000004"),
                column: "description",
                value: "Báo cáo trùng lặp được gộp vào báo cáo gốc");
        }
    }
}
