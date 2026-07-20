using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _202607150210_AddBlockedWords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blocked_words",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blocked_words", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "blocked_words",
                columns: new[] { "id", "created_at", "created_by", "is_active", "note", "updated_at", "updated_by", "word" },
                values: new object[,]
                {
                    { new Guid("b1000001-0000-0000-0000-000000000001"), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, null, "địt" },
                    { new Guid("b1000001-0000-0000-0000-000000000002"), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, null, "đụ" },
                    { new Guid("b1000001-0000-0000-0000-000000000003"), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, null, "lồn" },
                    { new Guid("b1000001-0000-0000-0000-000000000004"), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, null, "cặc" },
                    { new Guid("b1000001-0000-0000-0000-000000000005"), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, null, "đéo" },
                    { new Guid("b1000001-0000-0000-0000-000000000006"), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, null, "vcl" },
                    { new Guid("b1000001-0000-0000-0000-000000000007"), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, null, "vl" },
                    { new Guid("b1000001-0000-0000-0000-000000000008"), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, null, "fuck" },
                    { new Guid("b1000001-0000-0000-0000-000000000009"), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, null, "shit" },
                    { new Guid("b1000001-0000-0000-0000-00000000000a"), new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, null, "bitch" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_blocked_words_is_active",
                table: "blocked_words",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_blocked_words_word",
                table: "blocked_words",
                column: "word",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocked_words");
        }
    }
}
