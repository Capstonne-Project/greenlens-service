using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202607241800_AddIdentityNumberAndSatisfactionUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_violating_entities_identity_number",
                table: "violating_entities");

            migrationBuilder.CreateIndex(
                name: "ix_violating_entities_identity_number",
                table: "violating_entities",
                column: "identity_number",
                unique: true,
                filter: "identity_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_report_satisfactions_report_id_user_id",
                table: "report_satisfactions",
                columns: new[] { "report_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_violating_entities_identity_number",
                table: "violating_entities");

            migrationBuilder.DropIndex(
                name: "ix_report_satisfactions_report_id_user_id",
                table: "report_satisfactions");

            migrationBuilder.CreateIndex(
                name: "ix_violating_entities_identity_number",
                table: "violating_entities",
                column: "identity_number",
                filter: "identity_number IS NOT NULL");
        }
    }
}
