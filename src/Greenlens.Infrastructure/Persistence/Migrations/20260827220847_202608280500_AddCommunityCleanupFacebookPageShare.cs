using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202608280500_AddCommunityCleanupFacebookPageShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "facebook_page_shared_at",
                table: "community_cleanup_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "facebook_page_url",
                table: "community_cleanup_events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "facebook_post_id",
                table: "community_cleanup_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "facebook_page_shared_at",
                table: "community_cleanup_events");

            migrationBuilder.DropColumn(
                name: "facebook_page_url",
                table: "community_cleanup_events");

            migrationBuilder.DropColumn(
                name: "facebook_post_id",
                table: "community_cleanup_events");
        }
    }
}
