using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202608091930_AddAssignmentProgressUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "progress_update_id",
                table: "report_media",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "assignment_progress_updates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    progress_percent = table.Column<int>(type: "integer", nullable: false),
                    progress_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignment_progress_updates", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignment_progress_updates_report_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "report_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assignment_progress_updates_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_media_progress_update_id",
                table: "report_media",
                column: "progress_update_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_progress_updates_assignment_id",
                table: "assignment_progress_updates",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_progress_updates_assignment_id_created_at",
                table: "assignment_progress_updates",
                columns: new[] { "assignment_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_assignment_progress_updates_report_id",
                table: "assignment_progress_updates",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_progress_updates_updated_by_user_id",
                table: "assignment_progress_updates",
                column: "updated_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_report_media_assignment_progress_updates_progress_update_id",
                table: "report_media",
                column: "progress_update_id",
                principalTable: "assignment_progress_updates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_report_media_assignment_progress_updates_progress_update_id",
                table: "report_media");

            migrationBuilder.DropTable(
                name: "assignment_progress_updates");

            migrationBuilder.DropIndex(
                name: "ix_report_media_progress_update_id",
                table: "report_media");

            migrationBuilder.DropColumn(
                name: "progress_update_id",
                table: "report_media");
        }
    }
}
