using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202608241530_AddTeamWasteTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "team_waste_tags",
                columns: table => new
                {
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    waste_tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_waste_tags", x => new { x.team_id, x.waste_tag_id });
                    table.ForeignKey(
                        name: "fk_team_waste_tags_environmental_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "environmental_teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_team_waste_tags_waste_tags_waste_tag_id",
                        column: x => x.waste_tag_id,
                        principalTable: "waste_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_team_waste_tags_waste_tag_id",
                table: "team_waste_tags",
                column: "waste_tag_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_waste_tags");
        }
    }
}
