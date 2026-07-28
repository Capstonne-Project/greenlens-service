using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "community_cleanup_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_leo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leader_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leader_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    join_opens_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    join_closes_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_participants = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    meeting_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    meeting_latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    meeting_longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    progress_percent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    progress_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    progress_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by_leo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_cleanup_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_community_cleanup_events_environmental_teams_leader_team_id",
                        column: x => x.leader_team_id,
                        principalTable: "environmental_teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_community_cleanup_events_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_community_cleanup_events_users_created_by_leo_id",
                        column: x => x.created_by_leo_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_community_cleanup_events_users_leader_user_id",
                        column: x => x.leader_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "community_cleanup_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    check_in_latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    check_in_longitude = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_cleanup_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_community_cleanup_participants_community_cleanup_events_eve",
                        column: x => x.event_id,
                        principalTable: "community_cleanup_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_community_cleanup_participants_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_community_cleanup_events_active_per_report",
                table: "community_cleanup_events",
                column: "report_id",
                unique: true,
                filter: "status NOT IN ('Completed', 'Cancelled') AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_community_cleanup_events_created_by_leo_id",
                table: "community_cleanup_events",
                column: "created_by_leo_id");

            migrationBuilder.CreateIndex(
                name: "ix_community_cleanup_events_leader_team_id",
                table: "community_cleanup_events",
                column: "leader_team_id");

            migrationBuilder.CreateIndex(
                name: "ix_community_cleanup_events_leader_user_id_status",
                table: "community_cleanup_events",
                columns: new[] { "leader_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_community_cleanup_events_status_starts_at",
                table: "community_cleanup_events",
                columns: new[] { "status", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_community_cleanup_participants_event_id",
                table: "community_cleanup_participants",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_community_cleanup_participants_event_id_user_id",
                table: "community_cleanup_participants",
                columns: new[] { "event_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_community_cleanup_participants_user_id",
                table: "community_cleanup_participants",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "community_cleanup_participants");

            migrationBuilder.DropTable(
                name: "community_cleanup_events");
        }
    }
}
