using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202608251130_AddBadgeRequiredActionCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "required_action_count",
                table: "badges",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000001"),
                column: "required_action_count",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000002"),
                column: "required_action_count",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000003"),
                column: "required_action_count",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000004"),
                column: "required_action_count",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000005"),
                column: "required_action_count",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000006"),
                column: "required_action_count",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000008"),
                column: "required_action_count",
                value: 5);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000009"),
                column: "required_action_count",
                value: 10);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000010"),
                column: "required_action_count",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000011"),
                column: "required_action_count",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000012"),
                column: "required_action_count",
                value: null);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "id",
                keyValue: new Guid("a1000001-0000-0000-0000-000000000013"),
                column: "required_action_count",
                value: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "required_action_count",
                table: "badges");
        }
    }
}
