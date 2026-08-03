using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202608011200_AddNotificationDispatchTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "email_dispatched_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "push_dispatched_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_dispatched_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "push_dispatched_at",
                table: "notifications");
        }
    }
}
