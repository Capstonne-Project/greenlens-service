using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _20260518_AddAssignedStatusAndProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "progress_percent",
                table: "report_assignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "progress_note",
                table: "report_assignments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "progress_updated_at",
                table: "report_assignments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "progress_percent",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "progress_note",
                table: "report_assignments");

            migrationBuilder.DropColumn(
                name: "progress_updated_at",
                table: "report_assignments");
        }
    }
}
