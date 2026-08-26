using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchedByOfficerIdToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "dispatched_by_officer_id",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_reports_dispatched_by_officer_id",
                table: "reports",
                column: "dispatched_by_officer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reports_users_dispatched_by_officer_id",
                table: "reports",
                column: "dispatched_by_officer_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Backfill: legacy rows dispatched before this column existed — best-effort from verifying LEO.
            migrationBuilder.Sql("""
                UPDATE reports
                SET dispatched_by_officer_id = verified_by
                WHERE dispatched_to_company_at IS NOT NULL
                  AND dispatched_by_officer_id IS NULL
                  AND verified_by IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reports_users_dispatched_by_officer_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "ix_reports_dispatched_by_officer_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "dispatched_by_officer_id",
                table: "reports");
        }
    }
}
