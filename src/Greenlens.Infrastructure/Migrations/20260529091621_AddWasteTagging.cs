using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWasteTagging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ai_suggested_waste_tag_codes",
                table: "reports",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "waste_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_vi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icon_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waste_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_waste_tags",
                columns: table => new
                {
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    waste_tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tagged_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tagged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_waste_tags", x => new { x.report_id, x.waste_tag_id });
                    table.ForeignKey(
                        name: "fk_report_waste_tags_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_report_waste_tags_users_tagged_by_id",
                        column: x => x.tagged_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_report_waste_tags_waste_tags_waste_tag_id",
                        column: x => x.waste_tag_id,
                        principalTable: "waste_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "waste_tags",
                columns: new[] { "id", "code", "created_at", "description", "display_order", "icon_url", "is_active", "name_en", "name_vi" },
                values: new object[,]
                {
                    { new Guid("0ce80d1c-c18c-6659-bf72-7cf11da0ab50"), "RECYCLABLE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chai PET, lon nhôm, carton, giấy, thủy tinh", 3, null, true, "Recyclable", "Tái chế" },
                    { new Guid("0fe341a2-447c-2d5d-854c-08094907923e"), "HAZARDOUS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pin, bình hóa chất, sơn, dầu nhớt, thuốc trừ sâu", 6, null, true, "Hazardous", "Nguy hại" },
                    { new Guid("142cf16b-4bfa-8052-9d37-c54b4f777257"), "ANIMAL_CARCASS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chuột, chó mèo bị xe cán, gia cầm chết", 10, null, true, "Animal Carcass", "Xác động vật" },
                    { new Guid("1cc2956d-5b96-c453-9823-6a4422c95cf9"), "TEXTILE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quần áo cũ, vải vụn, thảm, rèm cửa", 11, null, true, "Textile", "Vải, quần áo" },
                    { new Guid("2502c58b-22fd-565a-a0b1-9c2467803d4f"), "ELECTRONIC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Điện thoại, dây cáp, bảng mạch, TV, máy tính", 5, null, true, "Electronic Waste", "Rác điện tử" },
                    { new Guid("376dcb07-7199-fe55-808d-76cd884bb418"), "CONSTRUCTION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gạch, xi măng, tấm lợp, ống nước, sắt thép", 7, null, true, "Construction Debris", "Phế thải xây dựng" },
                    { new Guid("5173c601-0868-d457-bb6e-a85c6c4da2ca"), "FOOD_ORGANIC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thức ăn thừa, rau củ hỏng, bã trà, rác vườn", 2, null, true, "Food & Organic", "Thực phẩm & Hữu cơ" },
                    { new Guid("82c9191f-338d-435b-8ea9-c1b4792adc32"), "BULKY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nệm, ghế sofa, tủ lạnh, bàn ghế, máy giặt", 8, null, true, "Bulky Items", "Đồ cồng kềnh" },
                    { new Guid("9a4d7cfd-020a-c755-bb98-39b91fbfd516"), "MEDICAL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khẩu trang, kim tiêm, băng gạc, thuốc hết hạn", 4, null, true, "Medical Waste", "Rác y tế" },
                    { new Guid("b46da84b-0149-8c5e-b378-0e9ad27d2da9"), "HOUSEHOLD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Túi nilon, quần áo cũ, rác hỗn hợp gia đình", 1, null, true, "Household Waste", "Rác sinh hoạt" },
                    { new Guid("f016f98a-ede8-2855-abad-509b0beae201"), "VEGETATION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cành cây, gốc cây, cỏ, lá khô số lượng lớn", 12, null, true, "Yard/Vegetation", "Cây cỏ, lá" },
                    { new Guid("ff25abe9-6df3-3c5f-8a0a-a28e9823bb6b"), "TIRE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lốp xe máy, ô tô, xe tải bị vứt bỏ", 9, null, true, "Tires", "Lốp xe" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_waste_tags_report_id",
                table: "report_waste_tags",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_waste_tags_tagged_by_id",
                table: "report_waste_tags",
                column: "tagged_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_waste_tags_waste_tag_id",
                table: "report_waste_tags",
                column: "waste_tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_waste_tags_code",
                table: "waste_tags",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_waste_tags");

            migrationBuilder.DropTable(
                name: "waste_tags");

            migrationBuilder.DropColumn(
                name: "ai_suggested_waste_tag_codes",
                table: "reports");
        }
    }
}
