using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeRequiredStreakDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "required_streak_days",
                table: "badges",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000001"),
                column: "required_streak_days",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000002"),
                column: "required_streak_days",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000003"),
                column: "required_streak_days",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000004"),
                column: "required_streak_days",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000005"),
                column: "required_streak_days",
                value: 7);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000006"),
                column: "required_streak_days",
                value: 30);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000007"),
                column: "required_streak_days",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000008"),
                column: "required_streak_days",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000009"),
                column: "required_streak_days",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000010"),
                column: "required_streak_days",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000011"),
                column: "required_streak_days",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000012"),
                column: "required_streak_days",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "required_streak_days",
                table: "badges");
        }
    }
}
