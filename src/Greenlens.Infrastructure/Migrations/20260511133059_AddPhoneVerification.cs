using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_phone_verified",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                table: "otp_codes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_otp_codes_phone_number_purpose_expires_at",
                table: "otp_codes",
                columns: new[] { "phone_number", "purpose", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_otp_codes_phone_number_purpose_expires_at",
                table: "otp_codes");

            migrationBuilder.DropColumn(
                name: "is_phone_verified",
                table: "users");

            migrationBuilder.DropColumn(
                name: "phone_number",
                table: "otp_codes");
        }
    }
}
