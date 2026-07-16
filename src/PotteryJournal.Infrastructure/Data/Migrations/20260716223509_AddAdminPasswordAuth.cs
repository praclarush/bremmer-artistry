using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPasswordAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "AllowedAdmins",
                schema: "potteryjournal",
                comment: "An admin account permitted to sign in to the admin area.",
                oldComment: "A Google account email permitted to sign in to the admin area.");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "potteryjournal",
                table: "AllowedAdmins",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                comment: "The admin's sign-in email.",
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldComment: "The allow-listed Google account email.");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                schema: "potteryjournal",
                table: "AllowedAdmins",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                comment: "PBKDF2 password hash produced by ASP.NET Core's PasswordHasher.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                schema: "potteryjournal",
                table: "AllowedAdmins");

            migrationBuilder.AlterTable(
                name: "AllowedAdmins",
                schema: "potteryjournal",
                comment: "A Google account email permitted to sign in to the admin area.",
                oldComment: "An admin account permitted to sign in to the admin area.");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "potteryjournal",
                table: "AllowedAdmins",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                comment: "The allow-listed Google account email.",
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldComment: "The admin's sign-in email.");
        }
    }
}
