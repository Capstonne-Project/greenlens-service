using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _202607100100_AddContractPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Create contract_periods table ──
            migrationBuilder.CreateTable(
                name: "contract_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contract_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    renewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_periods", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_periods_environmental_service_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "environmental_service_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_periods_company_id",
                table: "contract_periods",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_periods_company_id_start_date",
                table: "contract_periods",
                columns: new[] { "company_id", "start_date" });

            // ── Data migration: seed initial ContractPeriod for existing companies ──
            // Each existing company gets one ContractPeriod record snapshotting its current contract data.
            // renewed_by_user_id is set to '00000000-0000-0000-0000-000000000000' (system) for seeded records.
            migrationBuilder.Sql("""
                INSERT INTO contract_periods (id, company_id, contract_number, contract_type, start_date, end_date, renewed_by_user_id, note, created_at)
                SELECT
                    gen_random_uuid(),
                    c.id,
                    c.contract_number,
                    c.contract_type,
                    c.contract_start_date,
                    c.contract_end_date,
                    '00000000-0000-0000-0000-000000000000',
                    'Kỳ hợp đồng ban đầu (data migration)',
                    NOW()
                FROM environmental_service_companies c
                WHERE NOT EXISTS (
                    SELECT 1 FROM contract_periods cp WHERE cp.company_id = c.id
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_periods");
        }
    }
}
