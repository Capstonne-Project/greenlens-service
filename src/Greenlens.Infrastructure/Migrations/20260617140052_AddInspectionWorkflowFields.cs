using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "additional_penalty_measures",
                table: "inspection_reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_team_id",
                table: "inspection_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_repeat_offender",
                table: "inspection_reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "sla_inspection_due_at",
                table: "inspection_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "violation_level",
                table: "inspection_reports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_inspection_reports_assigned_team_id",
                table: "inspection_reports",
                column: "assigned_team_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_reports_violator_identity",
                table: "inspection_reports",
                column: "violator_identity");

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_reports_environmental_teams_assigned_team_id",
                table: "inspection_reports",
                column: "assigned_team_id",
                principalTable: "environmental_teams",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inspection_reports_environmental_teams_assigned_team_id",
                table: "inspection_reports");

            migrationBuilder.DropIndex(
                name: "ix_inspection_reports_assigned_team_id",
                table: "inspection_reports");

            migrationBuilder.DropIndex(
                name: "ix_inspection_reports_violator_identity",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "additional_penalty_measures",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "assigned_team_id",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "is_repeat_offender",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "sla_inspection_due_at",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "violation_level",
                table: "inspection_reports");
        }
    }
}
