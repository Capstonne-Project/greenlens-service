using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _202606112030_SimplifyCompanyContractModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "activation_token_expires_at",
                table: "environmental_service_companies");

            migrationBuilder.DropColumn(
                name: "activation_token_hash",
                table: "environmental_service_companies");

            migrationBuilder.AlterColumn<DateTime>(
                name: "contract_end_date",
                table: "environmental_service_companies",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "contract_end_date",
                table: "environmental_service_companies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "activation_token_expires_at",
                table: "environmental_service_companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "activation_token_hash",
                table: "environmental_service_companies",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
