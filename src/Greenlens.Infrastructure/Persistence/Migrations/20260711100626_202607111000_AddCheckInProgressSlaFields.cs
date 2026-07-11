using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202607111000_AddCheckInProgressSlaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "checked_in_at",
                table: "report_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "checked_in_latitude",
                table: "report_assignments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "checked_in_longitude",
                table: "report_assignments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "checked_in_note",
                table: "report_assignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "checked_in_at",
                table: "inspection_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "checked_in_latitude",
                table: "inspection_reports",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "checked_in_longitude",
                table: "inspection_reports",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "checked_in_note",
                table: "inspection_reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "progress_note",
                table: "inspection_reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "progress_percent",
                table: "inspection_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "progress_updated_at",
                table: "inspection_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sla_inspection_breached",
                table: "inspection_reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "checked_in_at",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "checked_in_latitude",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "checked_in_longitude",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "checked_in_note",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "checked_in_at",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "checked_in_latitude",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "checked_in_longitude",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "checked_in_note",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "progress_note",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "progress_percent",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "progress_updated_at",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "sla_inspection_breached",
                table: "inspection_reports");
        }
    }
}
