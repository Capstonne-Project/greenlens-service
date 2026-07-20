using System;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260720163000_AddCommentLikeAndReply")]
public class AddCommentLikeAndReply : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "parent_comment_id",
            table: "comments",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "comment_likes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                comment_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_comment_likes", x => x.id);
                table.ForeignKey(
                    name: "fk_comment_likes_comments_comment_id",
                    column: x => x.comment_id,
                    principalTable: "comments",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_comment_likes_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_comments_parent_comment_id",
            table: "comments",
            column: "parent_comment_id");

        migrationBuilder.CreateIndex(
            name: "ix_comment_likes_comment_id_user_id",
            table: "comment_likes",
            columns: new[] { "comment_id", "user_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_comment_likes_user_id",
            table: "comment_likes",
            column: "user_id");

        migrationBuilder.AddForeignKey(
            name: "fk_comments_comments_parent_comment_id",
            table: "comments",
            column: "parent_comment_id",
            principalTable: "comments",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_comments_comments_parent_comment_id",
            table: "comments");

        migrationBuilder.DropTable(
            name: "comment_likes");

        migrationBuilder.DropIndex(
            name: "ix_comments_parent_comment_id",
            table: "comments");

        migrationBuilder.DropColumn(
            name: "parent_comment_id",
            table: "comments");
    }
}
