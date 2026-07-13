using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202607121530_AddSoftDeleteToMediaAssignmentInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "report_media",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "report_media",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "report_media",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "report_media",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "report_media",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "report_media",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "report_assignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "report_assignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "report_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "report_assignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "report_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "report_assignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "inspection_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "inspection_reports",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "report_media");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "report_media");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "report_media");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "report_media");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "report_media");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "report_media");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "inspection_reports");
        }
    }
}
