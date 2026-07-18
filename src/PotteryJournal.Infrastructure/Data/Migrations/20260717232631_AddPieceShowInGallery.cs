using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPieceShowInGallery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Category",
                schema: "potteryjournal",
                table: "Pieces",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Free-text category tag used to group this piece on the Gallery page, when ShowInGallery is set.",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Free-text category tag used to build the Gallery page's category tiles.");

            migrationBuilder.AddColumn<bool>(
                name: "ShowInGallery",
                schema: "potteryjournal",
                table: "Pieces",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Whether this piece is curated for public display on the Gallery page. The Gallery is independent of the Pottery Journal -- a piece can be logged for record-keeping without ever appearing here.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowInGallery",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                schema: "potteryjournal",
                table: "Pieces",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Free-text category tag used to build the Gallery page's category tiles.",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Free-text category tag used to group this piece on the Gallery page, when ShowInGallery is set.");
        }
    }
}
