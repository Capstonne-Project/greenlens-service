using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202607121540_AddSoftDeleteToGamificationOrgCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "waste_tags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "waste_tags",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "waste_tags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "waste_tags",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "waste_tags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "user_points",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "user_points",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "user_points",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "user_points",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "user_points",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "pollution_categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "pollution_categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "pollution_categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "pollution_categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "pollution_categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "point_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "point_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "point_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "point_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "point_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "environmental_teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "environmental_teams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "environmental_service_companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "environmental_service_companies",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("0ce80d1c-c18c-6659-bf72-7cf11da0ab50"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("0fe341a2-447c-2d5d-854c-08094907923e"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("142cf16b-4bfa-8052-9d37-c54b4f777257"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("1cc2956d-5b96-c453-9823-6a4422c95cf9"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("2502c58b-22fd-565a-a0b1-9c2467803d4f"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("376dcb07-7199-fe55-808d-76cd884bb418"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("5173c601-0868-d457-bb6e-a85c6c4da2ca"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("82c9191f-338d-435b-8ea9-c1b4792adc32"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("9a4d7cfd-020a-c755-bb98-39b91fbfd516"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("b46da84b-0149-8c5e-b378-0e9ad27d2da9"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("f016f98a-ede8-2855-abad-509b0beae201"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "waste_tags",
                keyColumn: "id",
                keyValue: new Guid("ff25abe9-6df3-3c5f-8a0a-a28e9823bb6b"),
                columns: new[] { "created_by", "deleted_at", "deleted_by", "updated_at", "updated_by" },
                values: new object[] { null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                table: "waste_tags");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "waste_tags");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "waste_tags");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "waste_tags");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "waste_tags");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "user_points");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "user_points");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "user_points");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "user_points");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "user_points");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "pollution_categories");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "pollution_categories");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "pollution_categories");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "pollution_categories");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "pollution_categories");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "point_transactions");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "point_transactions");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "point_transactions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "point_transactions");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "point_transactions");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "environmental_teams");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "environmental_teams");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "environmental_service_companies");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "environmental_service_companies");
        }
    }
}
