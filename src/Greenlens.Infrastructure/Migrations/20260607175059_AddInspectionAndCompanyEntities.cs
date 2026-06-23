using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionAndCompanyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "environmental_service_companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    tax_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contract_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contract_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    contract_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    activation_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    activation_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_environmental_service_companies", x => x.id);
                    table.ForeignKey(
                        name: "fk_environmental_service_companies_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    violation_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    violator_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    violator_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    violator_identity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    penalty_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    penalty_decision_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    penalty_issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    penalty_due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    created_by_officer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_by_inspector_id = table.Column<Guid>(type: "uuid", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspection_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspection_reports_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inspection_reports_users_created_by_officer_id",
                        column: x => x.created_by_officer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspection_reports_users_issued_by_inspector_id",
                        column: x => x.issued_by_inspector_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "company_staff",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_staff", x => x.id);
                    table.ForeignKey(
                        name: "fk_company_staff_environmental_service_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "environmental_service_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_company_staff_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_company_staff_company_id",
                table: "company_staff",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_company_staff_is_active",
                table: "company_staff",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_company_staff_user_id_company_id",
                table: "company_staff",
                columns: new[] { "user_id", "company_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_environmental_service_companies_contract_number",
                table: "environmental_service_companies",
                column: "contract_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_environmental_service_companies_department_id",
                table: "environmental_service_companies",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_environmental_service_companies_status",
                table: "environmental_service_companies",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_environmental_service_companies_tax_code",
                table: "environmental_service_companies",
                column: "tax_code");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_reports_created_by_officer_id",
                table: "inspection_reports",
                column: "created_by_officer_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_reports_issued_by_inspector_id",
                table: "inspection_reports",
                column: "issued_by_inspector_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_reports_report_id",
                table: "inspection_reports",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_reports_status",
                table: "inspection_reports",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_staff");

            migrationBuilder.DropTable(
                name: "inspection_reports");

            migrationBuilder.DropTable(
                name: "environmental_service_companies");
        }
    }
}
