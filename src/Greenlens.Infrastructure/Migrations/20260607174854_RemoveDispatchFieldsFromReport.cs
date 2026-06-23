using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDispatchFieldsFromReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reports_users_dispatched_by_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "ix_reports_assigned_officer_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "ix_reports_dispatched_by_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "assigned_officer_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "dispatched_at",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "dispatched_by_id",
                table: "reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assigned_officer_id",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "dispatched_at",
                table: "reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "dispatched_by_id",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_reports_assigned_officer_id",
                table: "reports",
                column: "assigned_officer_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_dispatched_by_id",
                table: "reports",
                column: "dispatched_by_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reports_users_dispatched_by_id",
                table: "reports",
                column: "dispatched_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
