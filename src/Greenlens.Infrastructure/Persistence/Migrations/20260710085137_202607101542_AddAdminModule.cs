using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202607101542_AddAdminModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "hidden_at",
                table: "reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "hidden_by",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hidden_reason",
                table: "reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_hidden",
                table: "reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    old_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "gamification_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gamification_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title_vi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body_vi = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    title_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    body_en = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "penalty_frameworks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    violation_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    min_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    max_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_penalty_frameworks", x => x.id);
                    table.ForeignKey(
                        name: "fk_penalty_frameworks_pollution_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "pollution_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "gamification_configs",
                columns: new[] { "id", "action_type", "created_at", "created_by", "description", "is_active", "points", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000001"), "ReportVerified", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "Báo cáo được xác minh bởi LEO", true, 10, null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000002"), "ReportResolved", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "Báo cáo đã xử lý xong (cleanup hoàn tất)", true, 20, null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000003"), "PenaltyIssued", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "Biên bản xử phạt được ban hành", true, 20, null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000004"), "DuplicateReport", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "Báo cáo trùng lặp được gộp vào báo cáo gốc", true, 5, null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000005"), "ReportRejected", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "Báo cáo bị từ chối (không hợp lệ)", true, -5, null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000006"), "FraudPenalty", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "BR-GAM-006: Phạt gian lận — trừ toàn bộ điểm", true, -100, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_date",
                table: "audit_logs",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_periods_company_id",
                table: "contract_periods",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_periods_company_id_start_date",
                table: "contract_periods",
                columns: new[] { "company_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_gamification_config_action_type",
                table: "gamification_configs",
                column: "action_type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_template_key_channel",
                table: "notification_templates",
                columns: new[] { "template_key", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_penalty_fw_category_level_active",
                table: "penalty_frameworks",
                columns: new[] { "category_id", "violation_level", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "contract_periods");

            migrationBuilder.DropTable(
                name: "gamification_configs");

            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropTable(
                name: "penalty_frameworks");

            migrationBuilder.DropColumn(
                name: "hidden_at",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "hidden_by",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "hidden_reason",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "is_hidden",
                table: "reports");
        }
    }
}
