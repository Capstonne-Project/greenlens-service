using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "department_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "local_office_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_department_id",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_office_id",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    province_code = table.Column<string>(type: "character(2)", maxLength: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                    table.ForeignKey(
                        name: "fk_departments_provinces_province_code",
                        column: x => x.province_code,
                        principalTable: "provinces",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "local_offices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ward_code = table.Column<string>(type: "character(5)", maxLength: 5, nullable: false),
                    officer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_onboarded = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_local_offices", x => x.id);
                    table.ForeignKey(
                        name: "fk_local_offices_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_local_offices_users_officer_id",
                        column: x => x.officer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_local_offices_wards_ward_code",
                        column: x => x.ward_code,
                        principalTable: "wards",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "environmental_teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    local_office_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_environmental_teams", x => x.id);
                    table.ForeignKey(
                        name: "fk_environmental_teams_local_offices_local_office_id",
                        column: x => x.local_office_id,
                        principalTable: "local_offices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_leader = table.Column<bool>(type: "boolean", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_team_members_environmental_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "environmental_teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_team_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_department_id",
                table: "users",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_local_office_id",
                table: "users",
                column: "local_office_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_assigned_department_id",
                table: "reports",
                column: "assigned_department_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_assigned_office_id",
                table: "reports",
                column: "assigned_office_id");

            migrationBuilder.CreateIndex(
                name: "ix_departments_province_code",
                table: "departments",
                column: "province_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_environmental_teams_local_office_id",
                table: "environmental_teams",
                column: "local_office_id");

            migrationBuilder.CreateIndex(
                name: "ix_environmental_teams_team_type",
                table: "environmental_teams",
                column: "team_type");

            migrationBuilder.CreateIndex(
                name: "ix_local_offices_department_id",
                table: "local_offices",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_local_offices_officer_id",
                table: "local_offices",
                column: "officer_id");

            migrationBuilder.CreateIndex(
                name: "ix_local_offices_ward_code",
                table: "local_offices",
                column: "ward_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_team_members_team_id_user_id",
                table: "team_members",
                columns: new[] { "team_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_team_members_user_id",
                table: "team_members",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reports_departments_assigned_department_id",
                table: "reports",
                column: "assigned_department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_reports_environmental_teams_assigned_team_id",
                table: "reports",
                column: "assigned_team_id",
                principalTable: "environmental_teams",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_reports_local_offices_assigned_office_id",
                table: "reports",
                column: "assigned_office_id",
                principalTable: "local_offices",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_users_departments_department_id",
                table: "users",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_users_local_offices_local_office_id",
                table: "users",
                column: "local_office_id",
                principalTable: "local_offices",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reports_departments_assigned_department_id",
                table: "reports");

            migrationBuilder.DropForeignKey(
                name: "fk_reports_environmental_teams_assigned_team_id",
                table: "reports");

            migrationBuilder.DropForeignKey(
                name: "fk_reports_local_offices_assigned_office_id",
                table: "reports");

            migrationBuilder.DropForeignKey(
                name: "fk_users_departments_department_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "fk_users_local_offices_local_office_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "team_members");

            migrationBuilder.DropTable(
                name: "environmental_teams");

            migrationBuilder.DropTable(
                name: "local_offices");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropIndex(
                name: "ix_users_department_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_local_office_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_reports_assigned_department_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "ix_reports_assigned_office_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "department_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "local_office_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "assigned_department_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "assigned_office_id",
                table: "reports");
        }
    }
}
