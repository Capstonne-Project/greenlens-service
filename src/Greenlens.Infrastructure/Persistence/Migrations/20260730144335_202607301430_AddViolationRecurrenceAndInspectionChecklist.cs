using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202607301430_AddViolationRecurrenceAndInspectionChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_suspected_violation_recurrence",
                table: "reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "suspected_recurrence_of_report_id",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "checked_in_note",
                table: "inspection_reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "accepted_at",
                table: "inspection_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "accepted_by_user_id",
                table: "inspection_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "arrival_confirmed_at",
                table: "inspection_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "arrival_latitude",
                table: "inspection_reports",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "arrival_longitude",
                table: "inspection_reports",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "arrival_note",
                table: "inspection_reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "field_investigation_submitted_at",
                table: "inspection_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "field_investigation_submitted_by_user_id",
                table: "inspection_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inspection_evidences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    media_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspection_evidences", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspection_evidences_inspection_reports_inspection_report_id",
                        column: x => x.inspection_report_id,
                        principalTable: "inspection_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reports_is_suspected_violation_recurrence",
                table: "reports",
                column: "is_suspected_violation_recurrence");

            migrationBuilder.CreateIndex(
                name: "ix_reports_status_closed_at_category_id",
                table: "reports",
                columns: new[] { "status", "closed_at", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_suspected_recurrence_of_report_id",
                table: "reports",
                column: "suspected_recurrence_of_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_evidences_inspection_report_id",
                table: "inspection_evidences",
                column: "inspection_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_evidences_inspection_report_id_category",
                table: "inspection_evidences",
                columns: new[] { "inspection_report_id", "category" });

            migrationBuilder.AddForeignKey(
                name: "fk_reports_reports_suspected_recurrence_of_report_id",
                table: "reports",
                column: "suspected_recurrence_of_report_id",
                principalTable: "reports",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reports_reports_suspected_recurrence_of_report_id",
                table: "reports");

            migrationBuilder.DropTable(
                name: "inspection_evidences");

            migrationBuilder.DropIndex(
                name: "ix_reports_is_suspected_violation_recurrence",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "ix_reports_status_closed_at_category_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "ix_reports_suspected_recurrence_of_report_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "is_suspected_violation_recurrence",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "suspected_recurrence_of_report_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "accepted_at",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "accepted_by_user_id",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "arrival_confirmed_at",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "arrival_latitude",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "arrival_longitude",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "arrival_note",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "field_investigation_submitted_at",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "field_investigation_submitted_by_user_id",
                table: "inspection_reports");

            migrationBuilder.AlterColumn<string>(
                name: "checked_in_note",
                table: "inspection_reports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
