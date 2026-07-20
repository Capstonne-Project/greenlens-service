using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _20260519_RefactorReportColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reports_environmental_teams_assigned_team_id",
                table: "reports");

            migrationBuilder.RenameColumn(
                name: "assigned_team_id",
                table: "reports",
                newName: "assigned_by_officer_id");

            migrationBuilder.RenameIndex(
                name: "ix_reports_assigned_team_id",
                table: "reports",
                newName: "ix_reports_assigned_by_officer_id");

            // progress_note, progress_percent, progress_updated_at already exist from
            // 20260518_AddAssignedStatusAndProgress — only add the new column
            migrationBuilder.AddColumn<Guid>(
                name: "progress_updated_by_user_id",
                table: "report_assignments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "progress_updated_by_user_id",
                table: "report_assignments");

            migrationBuilder.RenameColumn(
                name: "assigned_by_officer_id",
                table: "reports",
                newName: "assigned_team_id");

            migrationBuilder.RenameIndex(
                name: "ix_reports_assigned_by_officer_id",
                table: "reports",
                newName: "ix_reports_assigned_team_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reports_environmental_teams_assigned_team_id",
                table: "reports",
                column: "assigned_team_id",
                principalTable: "environmental_teams",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
