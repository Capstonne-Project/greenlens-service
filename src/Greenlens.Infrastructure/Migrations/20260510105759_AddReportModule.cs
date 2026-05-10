using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pollution_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_vi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icon_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pollution_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_drafts", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_drafts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    severity_set_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ward_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    province_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assigned_team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_officer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reporter_count = table.Column<int>(type: "integer", nullable: false),
                    is_suspicious = table.Column<bool>(type: "boolean", nullable: false),
                    suspicious_reasons = table.Column<string>(type: "jsonb", nullable: true),
                    ai_pending = table.Column<bool>(type: "boolean", nullable: false),
                    ai_classified_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ai_confidence = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    ai_estimated_severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    priority_score = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reopened_count = table.Column<int>(type: "integer", nullable: false),
                    sla_verify_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sla_resolve_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_reports_pollution_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "pollution_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reports_reports_parent_report_id",
                        column: x => x.parent_report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reports_users_reporter_id",
                        column: x => x.reporter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reports_users_verified_by",
                        column: x => x.verified_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "report_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flagger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flag_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_flags", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_flags_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_report_flags_users_flagger_id",
                        column: x => x.flagger_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mime_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    p_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    exif_data = table.Column<string>(type: "jsonb", nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_media", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_media_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_report_media_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "report_satisfactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_satisfied = table.Column<bool>(type: "boolean", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_satisfactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_satisfactions_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_report_satisfactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    to_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_status_history_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_report_status_history_users_changed_by",
                        column: x => x.changed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pollution_categories_code",
                table: "pollution_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_drafts_user_id",
                table: "report_drafts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_flags_flagger_id",
                table: "report_flags",
                column: "flagger_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_flags_report_id_flagger_id_flag_type",
                table: "report_flags",
                columns: new[] { "report_id", "flagger_id", "flag_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_media_p_hash",
                table: "report_media",
                column: "p_hash");

            migrationBuilder.CreateIndex(
                name: "ix_report_media_report_id",
                table: "report_media",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_media_uploaded_by",
                table: "report_media",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "ix_report_satisfactions_report_id",
                table: "report_satisfactions",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_satisfactions_user_id",
                table: "report_satisfactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_status_history_changed_by",
                table: "report_status_history",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "ix_report_status_history_created_at",
                table: "report_status_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_report_status_history_report_id",
                table: "report_status_history",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_assigned_officer_id",
                table: "reports",
                column: "assigned_officer_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_assigned_team_id",
                table: "reports",
                column: "assigned_team_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_category_id",
                table: "reports",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_code",
                table: "reports",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reports_created_at",
                table: "reports",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_reports_parent_report_id",
                table: "reports",
                column: "parent_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_province_code",
                table: "reports",
                column: "province_code");

            migrationBuilder.CreateIndex(
                name: "ix_reports_reporter_id",
                table: "reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_severity",
                table: "reports",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "ix_reports_status",
                table: "reports",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_reports_verified_by",
                table: "reports",
                column: "verified_by");

            migrationBuilder.CreateIndex(
                name: "ix_reports_ward_code",
                table: "reports",
                column: "ward_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_drafts");

            migrationBuilder.DropTable(
                name: "report_flags");

            migrationBuilder.DropTable(
                name: "report_media");

            migrationBuilder.DropTable(
                name: "report_satisfactions");

            migrationBuilder.DropTable(
                name: "report_status_history");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "pollution_categories");
        }
    }
}
