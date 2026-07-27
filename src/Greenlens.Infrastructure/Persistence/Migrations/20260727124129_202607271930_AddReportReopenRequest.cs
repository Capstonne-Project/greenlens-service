using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202607271930_AddReportReopenRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_pending_reopen_request",
                table: "reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "reopen_request_id",
                table: "report_media",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "report_reopen_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_reopen_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_reopen_requests_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_report_reopen_requests_users_requested_by",
                        column: x => x.requested_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_media_reopen_request_id",
                table: "report_media",
                column: "reopen_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_reopen_requests_report_id_status",
                table: "report_reopen_requests",
                columns: new[] { "report_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_report_reopen_requests_requested_at",
                table: "report_reopen_requests",
                column: "requested_at");

            migrationBuilder.CreateIndex(
                name: "ix_report_reopen_requests_requested_by",
                table: "report_reopen_requests",
                column: "requested_by");

            migrationBuilder.AddForeignKey(
                name: "fk_report_media_report_reopen_requests_reopen_request_id",
                table: "report_media",
                column: "reopen_request_id",
                principalTable: "report_reopen_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_report_media_report_reopen_requests_reopen_request_id",
                table: "report_media");

            migrationBuilder.DropTable(
                name: "report_reopen_requests");

            migrationBuilder.DropIndex(
                name: "ix_report_media_reopen_request_id",
                table: "report_media");

            migrationBuilder.DropColumn(
                name: "has_pending_reopen_request",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "reopen_request_id",
                table: "report_media");
        }
    }
}
