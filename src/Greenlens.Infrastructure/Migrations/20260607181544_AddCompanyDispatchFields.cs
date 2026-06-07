using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDispatchFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assigned_company_id",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "dispatched_to_company_at",
                table: "reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "environmental_teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contract_type",
                table: "environmental_service_companies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_reports_assigned_company_id",
                table: "reports",
                column: "assigned_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_environmental_teams_company_id",
                table: "environmental_teams",
                column: "company_id");

            migrationBuilder.AddForeignKey(
                name: "fk_environmental_teams_environmental_service_companies_company",
                table: "environmental_teams",
                column: "company_id",
                principalTable: "environmental_service_companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_reports_environmental_service_companies_assigned_company_id",
                table: "reports",
                column: "assigned_company_id",
                principalTable: "environmental_service_companies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_environmental_teams_environmental_service_companies_company",
                table: "environmental_teams");

            migrationBuilder.DropForeignKey(
                name: "fk_reports_environmental_service_companies_assigned_company_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "ix_reports_assigned_company_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "ix_environmental_teams_company_id",
                table: "environmental_teams");

            migrationBuilder.DropColumn(
                name: "assigned_company_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "dispatched_to_company_at",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "environmental_teams");

            migrationBuilder.DropColumn(
                name: "contract_type",
                table: "environmental_service_companies");
        }
    }
}
