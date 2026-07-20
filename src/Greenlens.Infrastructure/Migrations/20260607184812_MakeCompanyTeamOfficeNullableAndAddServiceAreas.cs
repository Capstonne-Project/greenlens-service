using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeCompanyTeamOfficeNullableAndAddServiceAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "local_office_id",
                table: "environmental_teams",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "company_service_areas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ward_code = table.Column<string>(type: "character(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_service_areas", x => x.id);
                    table.ForeignKey(
                        name: "fk_company_service_areas_environmental_service_companies_compa",
                        column: x => x.company_id,
                        principalTable: "environmental_service_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_company_service_areas_wards_ward_code",
                        column: x => x.ward_code,
                        principalTable: "wards",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_company_service_areas_company_id_ward_code",
                table: "company_service_areas",
                columns: new[] { "company_id", "ward_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_company_service_areas_ward_code",
                table: "company_service_areas",
                column: "ward_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_service_areas");

            migrationBuilder.AlterColumn<Guid>(
                name: "local_office_id",
                table: "environmental_teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
