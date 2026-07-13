using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202607122250_AddViolatingEntityAndPenaltyPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "violating_entity_id",
                table: "inspection_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "penalty_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    evidence_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_penalty_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_penalty_payments_inspection_reports_inspection_report_id",
                        column: x => x.inspection_report_id,
                        principalTable: "inspection_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_penalty_payments_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "violating_entities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tax_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    identity_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_violating_entities", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inspection_reports_violating_entity_id",
                table: "inspection_reports",
                column: "violating_entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_penalty_payments_inspection_report_id",
                table: "penalty_payments",
                column: "inspection_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_penalty_payments_recorded_by_user_id",
                table: "penalty_payments",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_violating_entities_identity_number",
                table: "violating_entities",
                column: "identity_number",
                filter: "identity_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_violating_entities_name",
                table: "violating_entities",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_violating_entities_tax_code",
                table: "violating_entities",
                column: "tax_code",
                unique: true,
                filter: "tax_code IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_reports_violating_entities_violating_entity_id",
                table: "inspection_reports",
                column: "violating_entity_id",
                principalTable: "violating_entities",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inspection_reports_violating_entities_violating_entity_id",
                table: "inspection_reports");

            migrationBuilder.DropTable(
                name: "penalty_payments");

            migrationBuilder.DropTable(
                name: "violating_entities");

            migrationBuilder.DropIndex(
                name: "ix_inspection_reports_violating_entity_id",
                table: "inspection_reports");

            migrationBuilder.DropColumn(
                name: "violating_entity_id",
                table: "inspection_reports");
        }
    }
}
