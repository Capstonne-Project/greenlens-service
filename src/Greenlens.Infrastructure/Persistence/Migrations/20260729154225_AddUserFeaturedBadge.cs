using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFeaturedBadge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "featured_badge_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_featured_badge_id",
                table: "users",
                column: "featured_badge_id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_badges_featured_badge_id",
                table: "users",
                column: "featured_badge_id",
                principalTable: "badges",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_badges_featured_badge_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_featured_badge_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "featured_badge_id",
                table: "users");
        }
    }
}
